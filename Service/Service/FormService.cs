using ClassLibrary;
using Dapper;
using DynamicForm.Helper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DynamicForm.Service.Service;

public class FormService : IFormService
{
    private readonly SqlConnection _con;
    private readonly IConfiguration _configuration;
    private readonly IFormFieldMasterService _formFieldMasterService;
    private readonly ISchemaService _schemaService;
    private readonly IFormFieldConfigService _formFieldConfigService;
    private readonly IFormDataService _formDataService;
    private readonly IDropdownService _dropdownService;
    
    public FormService(SqlConnection connection, IFormFieldMasterService formFieldMasterService, ISchemaService schemaService, IFormFieldConfigService formFieldConfigService, IDropdownService dropdownService, IFormDataService formDataService, IConfiguration configuration)
    {
        _con = connection;
        _configuration = configuration;
        _formFieldMasterService = formFieldMasterService;
        _schemaService = schemaService;
        _formFieldConfigService = formFieldConfigService;
        _formDataService = formDataService;
        _dropdownService = dropdownService;
        _excludeColumns = _configuration.GetSection("DropdownSqlSettings:ExcludeColumns").Get<List<string>>() ?? new();
    }
    
    private readonly List<string> _excludeColumns;

    /// <summary>
    /// 取得指定 SCHEMA_TYPE 下的表單資料清單，
    /// 已自動將下拉選欄位的值轉為顯示文字（OptionText）
    /// </summary>
    public FormListDataViewModel GetFormList()
    {
        // 1. 查詢主表（Form 設定 Master），根據 SCHEMA_TYPE 取得表單主設定
        var master = _formFieldMasterService.GetFormFieldMaster(TableSchemaQueryType.All);
        
        // [防呆] 找不到主表就直接回傳空的 ViewModel，避免後續 NullReference
        if (master == null) 
            return new FormListDataViewModel();

        // 2. 取得檢視表的所有欄位名稱（資料表的 Schema）
        var columns = _schemaService.GetFormFieldMaster(master.VIEW_TABLE_NAME);

        // 3. 取得該表單的所有欄位設定（包含型別、控制項型態等）
        var fieldConfigs = _formFieldConfigService.GetFormFieldConfig(master.BASE_TABLE_ID);

        // 4. 取得檢視表的所有原始資料（rawRows 為每列 Dictionary<string, object?>）
        var rawRows = _formDataService.GetRows(master.VIEW_TABLE_NAME);

        // 5. 將 rawRows 轉換為 FormDataRow（每列帶主鍵 Id 與所有欄位 Cell）
        //    同時收集所有資料列的主鍵 rowIds
        var rows = _dropdownService.ToFormDataRows(rawRows, master.PRIMARY_KEY, out var rowIds);

        // 6. 若無任何資料列，直接回傳結果，省略後面下拉選查詢
        if (!rowIds.Any())
            return new FormListDataViewModel { FormId = master.ID, Columns = columns, Rows = rows };
        
        // 7. 取得所有資料列的下拉選答案（一次查全部，不 N+1）
        var dropdownAnswers = _dropdownService.GetAnswers(rowIds);
        
        // 8. 取得所有 OptionId → OptionText 的對照表
        var optionTextMap = _dropdownService.GetOptionTextMap(dropdownAnswers);
        
        // 9. 將 rows 裡所有下拉選欄位的值由 OptionId 轉換為 OptionText（顯示文字）
        _dropdownService.ReplaceDropdownIdsWithTexts(rows, fieldConfigs, dropdownAnswers, optionTextMap);

        // 10. 組裝並回傳最終的 ViewModel
        return new FormListDataViewModel
        {
            FormId = master.ID,
            Columns = columns,
            Rows = rows
        };
    }

    /// <summary>
    /// 根據表單設定抓取主表欄位與現有資料（編輯時用）
    /// 只對主表進行欄位組裝，Dropdown 顯示選項答案
    /// </summary>
    public FormSubmissionViewModel GetFormSubmission(Guid formMasterId, string? rowId = null)
    {
        // 1. 查主設定
        var master = _formFieldMasterService.GetFormFieldMasterFromId(formMasterId);
        if (master == null)
            throw new InvalidOperationException($"FORM_FIELD_Master {formMasterId} not found");
        if (master.BASE_TABLE_ID is null)
            throw new InvalidOperationException("主表設定不完整");

        // 2. 取得主表欄位（只抓主表，不抓 view）
        var fields = GetFields(master.BASE_TABLE_ID.Value, TableSchemaQueryType.OnlyTable, master.BASE_TABLE_NAME);

        // 3. 撈主表實際資料（如果是編輯模式）
        IDictionary<string, object?>? dataRow = null;
        Dictionary<Guid, Guid>? dropdownAnswers = null;

        if (!string.IsNullOrWhiteSpace(rowId))
        {
            // 3.1 取得主表主鍵名稱/型別/值
            var (pkName, pkType, pkValue) = FindPk(master, rowId);

            // 3.2 查詢主表資料（參數化防注入）
            var sql = $"SELECT * FROM [{master.BASE_TABLE_NAME}] WHERE [{pkName}] = @id";
            dataRow = _con.QueryFirstOrDefault(sql, new { id = pkValue }) as IDictionary<string, object?>;

            // 3.3 如果有Dropdown欄位，再查一次答案
            if (fields.Any(f => f.CONTROL_TYPE == FormControlType.Dropdown))
            {
                dropdownAnswers = _con.Query<(Guid FieldId, Guid OptionId)>(
                    @"/**/SELECT FORM_FIELD_CONFIG_ID AS FieldId, FORM_FIELD_DROPDOWN_OPTIONS_ID AS OptionId 
                      FROM FORM_FIELD_DROPDOWN_ANSWER WHERE ROW_ID = @RowId",
                    new { RowId = rowId })
                    .ToDictionary(x => x.FieldId, x => x.OptionId);
            }
        }

        // 4. 組裝欄位現值
        foreach (var field in fields)
        {
            if (field.CONTROL_TYPE == FormControlType.Dropdown && dropdownAnswers?.TryGetValue(field.FieldConfigId, out var optId) == true)
            {
                field.CurrentValue = optId;
            }
            else if (dataRow?.TryGetValue(field.Column, out var val) == true)
            {
                field.CurrentValue = val;
            }
            // else 預設 null（新增模式或沒有資料）
        }

        // 5. 回傳組裝後 ViewModel
        return new FormSubmissionViewModel
        {
            FormId = master.ID,
            RowId = rowId,
            TargetTableToUpsert = master.BASE_TABLE_NAME,
            FormName = master.FORM_NAME,
            Fields = fields
        };
    }
    
    /// <summary>
    /// 取得 欄位
    /// </summary>
    /// <param name="masterId"></param>
    /// <returns></returns>
   private List<FormFieldInputViewModel> GetFields(Guid masterId, TableSchemaQueryType schemaType, string tableName)
    {
        var columnTypes = _formDataService.LoadColumnTypes(tableName);
        var configData = _formFieldConfigService.LoadFieldConfigData(masterId);

        return configData.FieldConfigs
            .Select(cfg => BuildFieldViewModel(cfg, configData, columnTypes, schemaType))
            .ToList();
    }

    private FormFieldInputViewModel BuildFieldViewModel(
        FormFieldConfigDto field,
        FieldConfigData data,
        Dictionary<string, string> columnTypes,
        TableSchemaQueryType schemaType)
    {
        var dropdown = data.DropdownConfigs.FirstOrDefault(d => d.FORM_FIELD_CONFIG_ID == field.ID);
        var isUseSql = dropdown?.ISUSESQL ?? false;
        var dropdownId = dropdown?.ID ?? Guid.Empty;

        var options = data.DropdownOptions.Where(o => o.FORM_FIELD_DROPDOWN_ID == dropdownId).ToList();
        var finalOptions = isUseSql
            ? options.Where(x => !string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList()
            : options.Where(x => string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList();

        var rules = data.ValidationRules
            .Where(r => r.FIELD_CONFIG_ID == field.ID)
            .OrderBy(r => r.VALIDATION_ORDER)
            .ToList();

        var dataType = columnTypes.TryGetValue(field.COLUMN_NAME, out var dtype)
            ? dtype
            : "nvarchar";

        return new FormFieldInputViewModel
        {
            FieldConfigId = field.ID,
            Column = field.COLUMN_NAME,
            CONTROL_TYPE = field.CONTROL_TYPE,
            DefaultValue = field.DEFAULT_VALUE,
            IS_VISIBLE = field.IS_VISIBLE,
            IS_EDITABLE = field.IS_EDITABLE,
            ValidationRules = rules,
            OptionList = finalOptions,
            ISUSESQL = isUseSql,
            DROPDOWNSQL = dropdown?.DROPDOWNSQL ?? string.Empty,
            SOURCE = schemaType,
            DATA_TYPE = dataType,
            SOURCE_TABLE = TableSchemaQueryType.OnlyTable.ToString()
        };
    }

    /// <summary>
    /// 儲存或更新表單資料（含下拉選項答案）
    /// </summary>
    /// <param name="input">前端送出的表單資料</param>
    public void SubmitForm(FormSubmissionInputModel input)
    {
        var formId = input.FormId;
        var rowId = input.RowId;

        // 查表單主設定
        var master = _formFieldMasterService.GetFormFieldMasterFromId(formId);

        // 查欄位設定
        var configs = _con.Query<(Guid Id, string Column, int ControlType, string? DataType)>(
            "SELECT ID, COLUMN_NAME, CONTROL_TYPE, DATA_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @Id",
            new { Id = master.BASE_TABLE_ID }
        ).ToDictionary(c => c.Id);

        // 1. 欄位 mapping & 型別處理
        var (normalFields, dropdownAnswers) = MapInputFields(input.InputFields, configs);

        // 2. Insert/Update 決策
        var (pkName, pkType, typedRowId) = FindPk(master, rowId);
        bool isInsert = string.IsNullOrEmpty(rowId);
        bool isIdentity = IsIdentityColumn(master.BASE_TABLE_NAME, pkName);
        object? realRowId = typedRowId;

        if (isInsert)
            realRowId = InsertRow(master, pkName, pkType, isIdentity, normalFields);
        else
            UpdateRow(master, pkName, normalFields, realRowId);

        // 3. Dropdown Upsert
        foreach (var (configId, optionId) in dropdownAnswers)
            _con.Execute(Sql.UpsertDropdownAnswer, new { configId, RowId = realRowId, optionId });
    }

    private (List<(string Column, object? Value)> NormalFields, List<(Guid ConfigId, Guid OptionId)> DropdownAnswers)
        MapInputFields(IEnumerable<FormInputField> inputFields,
            Dictionary<Guid, (Guid Id, string Column, int ControlType, string? DataType)> configs)
    {
        var normalFields = new List<(string Column, object? Value)>();
        var dropdownAnswers = new List<(Guid ConfigId, Guid OptionId)>();

        foreach (var field in inputFields)
        {
            if (!configs.TryGetValue(field.FieldConfigId, out var cfg))
                continue;

            if (!string.IsNullOrWhiteSpace(field.Column) &&
                !string.Equals(field.Column, cfg.Column, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if ((FormControlType)cfg.ControlType == FormControlType.Dropdown)
            {
                if (Guid.TryParse(field.Value, out var optionId))
                    dropdownAnswers.Add((cfg.Id, optionId));
            }
            else
            {
                var val = ConvertToColumnTypeHelper.Convert(cfg.DataType, field.Value);
                normalFields.Add((cfg.Column, val));
            }
        }

        return (normalFields, dropdownAnswers);
    }

    /// <summary>
    /// 動態產生 insert，支援 identity or 非 identity
    /// </summary>
    private object InsertRow(FORM_FIELD_Master master, string pkName, string pkType, bool isIdentity, List<(string Column, object? Value)> normalFields)
    {
        var columns = new List<string>();
        var values = new List<string>();
        var paramDict = new Dictionary<string, object>();

        if (!isIdentity)
        {
            var newId = GeneratePkValue(pkType);
            columns.Add($"[{pkName}]");
            values.Add("@ROWID");
            paramDict["ROWID"] = newId!;
        }

        int i = 0;
        foreach (var field in normalFields)
        {
            if (string.Equals(field.Column, pkName, StringComparison.OrdinalIgnoreCase))
                continue;
            var paramName = $"VAL{i}";
            columns.Add($"[{field.Column}]");
            values.Add($"@{paramName}");
            paramDict[paramName] = field.Value ?? null;
            i++;
        }

        string sql;
        object? resultId = null;
        if (isIdentity)
        {
            sql = $"INSERT INTO [{master.BASE_TABLE_NAME}] ({string.Join(", ", columns)}) OUTPUT INSERTED.[{pkName}] VALUES ({string.Join(", ", values)})";
            resultId = _con.ExecuteScalar(sql, paramDict);
        }
        else
        {
            sql = $"INSERT INTO [{master.BASE_TABLE_NAME}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
            _con.Execute(sql, paramDict);
            resultId = paramDict.ContainsKey("ROWID") ? paramDict["ROWID"] : null;
        }
        return resultId;
    }

    /// <summary>
    /// 動態產生 update
    /// </summary>
    private void UpdateRow(FORM_FIELD_Master master, string pkName, List<(string Column, object? Value)> normalFields, object realRowId)
    {
        if (!normalFields.Any()) return;
        var setList = new List<string>();
        var paramDict = new Dictionary<string, object> { ["ROWID"] = realRowId! };
        int i = 0;
        foreach (var field in normalFields)
        {
            var paramName = $"VAL{i}";
            setList.Add($"[{field.Column}] = @{paramName}");
            paramDict[paramName] = field.Value ?? null;
            i++;
        }
        var sql = $"UPDATE [{master.BASE_TABLE_NAME}] SET {string.Join(", ", setList)} WHERE [{master.PRIMARY_KEY}] = @ROWID";
        _con.Execute(sql, paramDict);
    }


    private bool IsIdentityColumn(string tableName, string columnName)
    {
        var sql = @"SELECT COLUMNPROPERTY(OBJECT_ID(@TableName), @ColumnName, 'IsIdentity') AS IsIdentity";
        var isIdentity = _con.ExecuteScalar<int>(sql, new { TableName = tableName, ColumnName = columnName });
        return isIdentity == 1;
    }
    
    // ========== 主鍵自動產生 Helper ==========
    private static object GeneratePkValue(string pkType)
    {
        switch (pkType.ToLower())
        {
            case "uniqueidentifier": return Guid.NewGuid();
            case "decimal": return RandomDecimalHelper.GenerateRandomDecimal();
            case "numeric": return 0m;
            case "bigint": return 0L;
            case "int": return 0;
            case "nvarchar":
            case "varchar":
            case "char": return Guid.NewGuid().ToString("N");
            default: throw new NotSupportedException($"不支援的主鍵型別: {pkType}");
        }
    }
    
    /// <summary>
    /// 動態查詢 view/table 的主鍵欄位名稱、型別，並將 id 轉型成正確型別
    /// </summary>
    private (string PkName, string PkType, object? Value) FindPk(FORM_FIELD_Master master, string? fromId)
    {
        // 組 LIKE 條件（如 "COLUMN_NAME LIKE '%ID%' OR COLUMN_NAME LIKE '%SID%'"）
        var likeConditions = _excludeColumns
            .Select(x => $"COLUMN_NAME LIKE '%{x}%'")
            .ToList();
        var whereClause = string.Join(" OR ", likeConditions);

        // 查出 pkName、pkType
        var sql = $@"
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = @TableName
  AND ({whereClause})
";
        var pkInfo = _con.QueryFirstOrDefault<(string ColumnName, string DataType)>(sql, new { TableName = master.VIEW_TABLE_NAME });

        if (pkInfo.Equals(default((string, string))))
            throw new Exception("找不到符合規則的主鍵！");

        // 動態轉換 fromId 型別
        object? idValue = fromId != null
            ? ConvertPkType(fromId, pkInfo.DataType)
            : null;

        return (pkInfo.ColumnName, pkInfo.DataType, idValue);
    }

    /// <summary>
    /// 根據 SQL 型別，將傳入 id 轉換為 DB 支援的型別
    /// </summary>
    private static object ConvertPkType(string? id, string pkType)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        switch (pkType.ToLower())
        {
            case "uniqueidentifier": return Guid.Parse(id.ToString());
            case "decimal":
            case "numeric": return Convert.ToDecimal(id);
            case "bigint": return Convert.ToInt64(id);
            case "int": return Convert.ToInt32(id);
            case "nvarchar":
            case "varchar":
            case "char": return id.ToString();
            default: throw new NotSupportedException($"不支援的型別: {pkType}");
        }
    }
    
    /* ---------- Visitors ---------- */

    /// <summary>掃 FROM / JOIN，建立 alias ➜ table 對照</summary>
    private sealed class TableAliasVisitor : TSqlFragmentVisitor
    {
        private readonly Dictionary<string, string> _map;
        public TableAliasVisitor(Dictionary<string, string> map) => _map = map;

        public override void Visit(NamedTableReference node)
        {
            if (node.Alias is not null)
                _map[node.Alias.Value] = node.SchemaObject.BaseIdentifier.Value;
        }
    }

    /// <summary>掃最外層 SELECT，依序收集欄位</summary>
    private sealed class ColumnVisitor : TSqlFragmentVisitor
    {
        private readonly Dictionary<string, string> _alias2Table;
        private bool _done;                 // 只抓第一個 QuerySpecification
        public List<(string Col, string? Tbl)> Columns { get; } = new();

        public ColumnVisitor(Dictionary<string, string> alias2Table) => _alias2Table = alias2Table;

        public override void Visit(QuerySpecification node)
        {
            if (_done) return;              // 只處理最外層
            _done = true;

            foreach (var elem in node.SelectElements)
            {
                if (elem is SelectScalarExpression sse &&
                    sse.Expression is ColumnReferenceExpression col)
                {
                    var ids = col.MultiPartIdentifier.Identifiers;
                    if (ids.Count < 2) continue; // 可能是單欄位或 *
                    string alias = ids[0].Value;
                    string colName = (sse.ColumnName?.Value) ?? ids[1].Value;
                    _alias2Table.TryGetValue(alias, out var tbl);

                    Columns.Add((colName, tbl)); // 順序 = 插入順序
                }
            }
        }
    }
private static class Sql
    {
        public const string UpsertDropdownAnswer = @"
MERGE FORM_FIELD_DROPDOWN_ANSWER AS target
USING (SELECT @ConfigId AS FORM_FIELD_CONFIG_ID, @RowId AS ROW_ID) AS src
    ON target.FORM_FIELD_CONFIG_ID = src.FORM_FIELD_CONFIG_ID AND target.ROW_ID = src.ROW_ID
WHEN MATCHED THEN
    UPDATE SET FORM_FIELD_DROPDOWN_OPTIONS_ID = @OptionId
WHEN NOT MATCHED THEN
    INSERT (ID, FORM_FIELD_CONFIG_ID, FORM_FIELD_DROPDOWN_OPTIONS_ID, ROW_ID)
    VALUES (NEWID(), src.FORM_FIELD_CONFIG_ID, @OptionId, src.ROW_ID);";
    }
}
