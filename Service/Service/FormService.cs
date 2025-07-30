using System.Text.RegularExpressions;
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
    /// 取得指定表單對應檢視表的所有資料
    /// 會將下拉選單欄位的值直接轉成對應顯示文字
    /// </summary>
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

        // // 6. 若無任何資料列，直接回傳結果，省略後面下拉選查詢
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
    /// 取得 單一
    /// </summary>
    /// <param name="id"></param>
    /// <param name="fromId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public FormSubmissionViewModel GetFormSubmission(Guid id, string? fromId = null)
    {
        // 1. 查 Master 設定
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id });
        if (master == null)
            throw new InvalidOperationException($"FORM_FIELD_Master {id} not found");

        if (master.BASE_TABLE_ID is null || master.VIEW_TABLE_ID is null)
            throw new InvalidOperationException("主表與檢視表 ID 不完整");

        var fields = GetFields(master.BASE_TABLE_ID.Value, TableSchemaQueryType.OnlyTable, master.BASE_TABLE_NAME);
        foreach (var f in fields)
        {
            f.SOURCE = TableSchemaQueryType.OnlyTable;
        }
        
        if (!string.IsNullOrWhiteSpace(master.PRIMARY_KEY)
            && !string.IsNullOrWhiteSpace(master.VIEW_TABLE_NAME)
            && fromId != null)
        {
            var (pkName, pkType, idValue) = FindPk(master, fromId);
            var sql = $"SELECT * FROM [{master.VIEW_TABLE_NAME}] WHERE [{pkName}] = @id";
            var dataRow = _con.QueryFirstOrDefault(sql, new { id = idValue });

            IDictionary<string, object?>? dict = null;
            if (dataRow is not null)
            {
                dict = (IDictionary<string, object?>)dataRow;
            }

            var dropdownAnswers = _con.Query<(Guid FieldId, Guid OptionId)>(
                "SELECT FORM_FIELD_CONFIG_ID AS FieldId, FORM_FIELD_DROPDOWN_OPTIONS_ID AS OptionId " +
                "FROM FORM_FIELD_DROPDOWN_ANSWER WHERE ROW_ID = @RowId",
                new { RowId = fromId })
                .ToDictionary(a => a.FieldId, a => a.OptionId);

            foreach (var field in fields)
            {
                if (field.CONTROL_TYPE == FormControlType.Dropdown)
                {
                    if (dropdownAnswers.TryGetValue(field.FieldConfigId, out var optionId))
                        field.CurrentValue = optionId;
                }
                else if (dict != null && dict.TryGetValue(field.COLUMN_NAME, out var val))
                {
                    field.CurrentValue = val;
                }
            }
        }

        return new FormSubmissionViewModel
        {
            FormId = master.ID,
            RowId = fromId,
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
        // 1. 查詢欄位型別
        var columnTypes = _con.Query<(string COLUMN_NAME, string DATA_TYPE)>(
            @"SELECT COLUMN_NAME, DATA_TYPE
          FROM INFORMATION_SCHEMA.COLUMNS
          WHERE TABLE_NAME = @TableName",
            new { TableName = tableName }
        ).ToDictionary(x => x.COLUMN_NAME, x => x.DATA_TYPE, StringComparer.OrdinalIgnoreCase);

        // 2. 取得來源表資訊（僅 View 需要）
        Dictionary<string, string?> sourceTableMap = schemaType == TableSchemaQueryType.OnlyView
            ? GetViewColumnSources(tableName)
            : columnTypes.Keys.ToDictionary(k => k, _ => tableName, StringComparer.OrdinalIgnoreCase);
        
        var sql = @"SELECT FFC.*, FFM.FORM_NAME
                    FROM FORM_FIELD_CONFIG FFC
                    JOIN FORM_FIELD_Master FFM ON FFM.ID = FFC.FORM_FIELD_Master_ID
                    WHERE FFM.ID = @ID
                    ORDER BY FIELD_ORDER;

                    SELECT R.*
                    FROM FORM_FIELD_VALIDATION_RULE R
                    JOIN FORM_FIELD_CONFIG C ON R.FIELD_CONFIG_ID = C.ID
                    WHERE C.FORM_FIELD_Master_ID = @ID;

                    SELECT D.*
                    FROM FORM_FIELD_DROPDOWN D
                    JOIN FORM_FIELD_CONFIG C ON D.FORM_FIELD_CONFIG_ID = C.ID
                    WHERE C.FORM_FIELD_Master_ID = @ID;

                    SELECT O.*
                    FROM FORM_FIELD_DROPDOWN_OPTIONS O
                    JOIN FORM_FIELD_DROPDOWN D ON O.FORM_FIELD_DROPDOWN_ID = D.ID
                    JOIN FORM_FIELD_CONFIG C ON D.FORM_FIELD_CONFIG_ID = C.ID
                    WHERE C.FORM_FIELD_Master_ID = @ID;";

        using var multi = _con.QueryMultiple(sql, new { ID = masterId });

        var fieldConfigs = multi.Read<FormFieldConfigDto>().ToList();
        var validationRules = multi.Read<FormFieldValidationRuleDto>().ToList();
        var dropdownConfigs = multi.Read<FORM_FIELD_DROPDOWN>().ToList();
        var dropdownOptions = multi.Read<FORM_FIELD_DROPDOWN_OPTIONS>().ToList();

        // 用 LINQ 聚合，不需要 Dictionary
        var fieldViewModels = fieldConfigs
            .Select(field =>
            {
                // 1. 找出對應的下拉選單設定
                var dropdown = dropdownConfigs.FirstOrDefault(d => d.FORM_FIELD_CONFIG_ID == field.ID);
                var isUseSql = dropdown?.ISUSESQL ?? false;
                var dropdownId = dropdown?.ID ?? Guid.Empty;

                // 2. 找出此 dropdown 下的所有 options
                var options = dropdownOptions.Where(o => o.FORM_FIELD_DROPDOWN_ID == dropdownId).ToList();

                // 3. 根據 isUseSql 決定 option 顯示邏輯
                var finalOptions = isUseSql
                    ? options.Where(x => !string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList()
                    : options.Where(x => string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList();

                // 4. 找出 validation rules
                var rules = validationRules
                    .Where(r => r.FIELD_CONFIG_ID == field.ID)
                    .OrderBy(r => r.VALIDATION_ORDER)
                    .ToList();

                // 取型別：找不到預設 nvarchar
                var dataType = columnTypes.TryGetValue(field.COLUMN_NAME, out var dtype)
                    ? dtype
                    : "nvarchar";
                
                return new FormFieldInputViewModel
                {
                    FieldConfigId = field.ID,
                    COLUMN_NAME = field.COLUMN_NAME,
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
                    SOURCE_TABLE = sourceTableMap.TryGetValue(field.COLUMN_NAME, out var src) ? src : tableName
                };
            })
            .ToList();

        return fieldViewModels;
    }

    /// <summary>
    /// 儲存或更新表單資料（含下拉選項答案）
    /// </summary>
    /// <param name="input">前端送出的表單資料</param>
    public void SubmitForm(FormSubmissionInputModel input)
    {
        var formId = input.FormId;
        var rowId = input.RowId;

        // 1. 查詢表單主設定，確認表單是否存在，以及相關設定是否完整
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id = formId });

        // 2. 查詢該表單所有欄位設定（FORM_FIELD_CONFIG）
        var configs = _con.Query<(Guid Id, string Column, int ControlType)>(
            "SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @Id",
            new { Id = master.BASE_TABLE_ID }
        ).ToDictionary(c => c.Id);

        // 3. 分類欄位答案（依照型態拆成兩組）
        // - normalFields: 一般型態（非下拉選）欄位，用於直接寫入主資料表
        // - dropdownAnswers: 下拉選單答案，需額外存到 FORM_FIELD_DROPDOWN_ANSWER 關聯表
        var normalFields = new List<(string Column, object? Value)>();
        var dropdownAnswers = new List<(Guid ConfigId, Guid OptionId)>();

        // 逐一處理前端傳來的欄位答案
        foreach (var field in input.InputFields)
        {
            // 欄位 ID 在設定中找不到則跳過
            if (!configs.TryGetValue(field.FieldConfigId, out var cfg))
                continue;

            // 如果是 Dropdown 型態
            if ((FormControlType)cfg.ControlType == FormControlType.Dropdown)
            {
                // Dropdown 值應該是 OptionId（Guid 字串），需驗證合法性
                if (Guid.TryParse(field.Value, out var optionId))
                    dropdownAnswers.Add((cfg.Id, optionId));
            }
            else
            {
                // 其他型態直接存（如 Text, Number, Date ...）
                normalFields.Add((cfg.Column, field.Value));
            }
        }

        // 4. 判斷 Insert 或 Update，並處理主鍵資料
        var (pkName, pkType, typedRowId) = FindPk(master, rowId);
        bool isInsert = string.IsNullOrEmpty(rowId);
        bool isIdentity = IsIdentityColumn(master.BASE_TABLE_NAME, pkName);
        object? realRowId = typedRowId;

        if (isInsert)
        {
            var columns = new List<string>();
            var values = new List<string>();
            var paramDict = new Dictionary<string, object>();

            if (!isIdentity)
            {
                realRowId = GeneratePkValue(pkType);
                columns.Add($"[{master.PRIMARY_KEY}]");
                values.Add("@ROWID");
                paramDict["ROWID"] = realRowId!;
            }

            int i = 0;
            foreach (var field in normalFields)
            {
                if (string.Equals(field.Column, master.PRIMARY_KEY, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                var paramName = $"VAL{i}";
                columns.Add($"[{field.Column}]");
                values.Add($"@{paramName}");
                paramDict[paramName] = field.Value;
                i++;
            }

            string sql;
            if (isIdentity)
            {
                sql = $"INSERT INTO [{master.BASE_TABLE_NAME}] ({string.Join(", ", columns)}) OUTPUT INSERTED.[{master.PRIMARY_KEY}] VALUES ({string.Join(", ", values)})";
                realRowId = _con.ExecuteScalar(sql, paramDict);
            }
            else
            {
                sql = $"INSERT INTO [{master.BASE_TABLE_NAME}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
                _con.Execute(sql, paramDict);
            }
        }
        else
        {
            realRowId = typedRowId;
            if (normalFields.Any())
            {
                var setList = new List<string>();
                var paramDict = new Dictionary<string, object> { ["ROWID"] = realRowId! };
                int i = 0;
                foreach (var field in normalFields)
                {
                    var paramName = $"VAL{i}";
                    setList.Add($"[{field.Column}] = @{paramName}");
                    paramDict[paramName] = field.Value;
                    i++;
                }

                var sql = $"UPDATE [{master.BASE_TABLE_NAME}] SET {string.Join(", ", setList)} WHERE [{master.PRIMARY_KEY}] = @ROWID";
                _con.Execute(sql, paramDict);
            }
        }

        // 5. 寫入/更新所有 Dropdown 選項答案（Upsert 到 FORM_FIELD_DROPDOWN_ANSWER）
        foreach (var (ConfigId, OptionId) in dropdownAnswers)
        {
            _con.Execute(Sql.UpsertDropdownAnswer, new { ConfigId, RowId = realRowId, OptionId });
        }
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
    
    /// <summary>
    /// 傳入 View 名稱，回傳欄位名稱與來源表對應（Key: 欄位名, Value: 來源表）
    /// </summary>
    public Dictionary<string, string?> GetViewColumnSources(string viewName)
    {
        // 1️⃣ 讀 View 定義
        const string sql = @"
            SELECT m.definition
            FROM sys.views v
            JOIN sys.sql_modules m ON m.object_id = v.object_id
            WHERE v.name = @viewName;";
        string? viewDef = _con.QueryFirstOrDefault<string>(sql, new { viewName });
        if (string.IsNullOrWhiteSpace(viewDef))
            throw new InvalidOperationException($"找不到 View：{viewName}");

        // 2️⃣ ScriptDom 解析
        var parser = new TSql150Parser(initialQuotedIdentifiers: false);
        using var sr = new StringReader(viewDef);
        var fragment = parser.Parse(sr, out var errors);
        if (errors.Count > 0)
            throw new InvalidOperationException($"SQL 解析失敗：{errors[0].Message}");

        // 3️⃣ 先收集「別名 ➜ 表名」
        var alias2Table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tblVisitor  = new TableAliasVisitor(alias2Table);
        fragment.Accept(tblVisitor);

        // 4️⃣ 再收集 SELECT 欄位（保持順序）
        var colVisitor = new ColumnVisitor(alias2Table);
        fragment.Accept(colVisitor);

        // 5️⃣ 轉成 Dictionary（插入順序即輸出順序）
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (col, tbl) in colVisitor.Columns)
            dict.Add(col, tbl);

        return dict;
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