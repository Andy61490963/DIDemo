using System.Text.RegularExpressions;
using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DynamicForm.Service.Service;

public class FormService : IFormService
{
    private readonly SqlConnection _con;
    private readonly IConfiguration _configuration;
    
    public FormService(SqlConnection connection, IConfiguration configuration)
    {
        _con = connection;
        _configuration = configuration;
        _excludeColumns = _configuration.GetSection("DropdownSqlSettings:ExcludeColumns").Get<List<string>>() ?? new();
    }
    
    private readonly List<string> _excludeColumns;
    /// <summary>
    /// 取得指定表單對應檢視表的所有資料
    /// 會將下拉選單欄位的值直接轉成對應顯示文字
    /// </summary>
    public FormListDataViewModel GetFormList()
    {
        // 1. 查詢該 SCHEMA_TYPE 下的主表設定（Master）
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
            new { TYPE = TableSchemaQueryType.All.ToInt() });

        // 找不到主表就回傳空結果
        if (master == null)
        {
            return new FormListDataViewModel();
        }

        // 2. 取得檢視表的所有欄位名稱
        var columns = _con.Query<string>(
            "/**/SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table",
            new { table = master.VIEW_TABLE_NAME }).ToList();

        // 3. 查出所有欄位的設定（包含下拉選欄位型別）
        var fieldConfigs = _con.Query<(Guid Id, string Column, int Type)>(
            "/**/SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
            new { id = master.BASE_TABLE_ID }).ToList();

        // 4. 將所有 Dropdown 欄位整理成列表，方便後續查找
        var dropdownColumns = fieldConfigs
            .Where(f => (FormControlType)f.Type == FormControlType.Dropdown)
            .Select(f => (f.Column, f.Id))
            .ToList();

        // 5. 查詢檢視表全部資料，並存成 List<FormDataRow>
        var rows = new List<FormDataRow>();
        
        // 存每一筆 row 的主鍵 ID
        var rowIds = new List<string>();
        var rawRows = _con.Query($"SELECT * FROM [{master.VIEW_TABLE_NAME}]");
        foreach (IDictionary<string, object?> row in rawRows)
        {
            var vmRow = new FormDataRow();
            foreach (var kv in row)
            {
                // 主鍵欄位要存到 vmRow.Id，方便之後查下拉選答案，比對 主表主鍵 和 設定的主鍵 有沒有相同(目前限制Guid)
                if (string.Equals(kv.Key, master.PRIMARY_KEY, StringComparison.OrdinalIgnoreCase))
                {
                    vmRow.Id = kv.Value?.ToString();
                    rowIds.Add(kv.Value?.ToString() ?? "");
                }

                // 其他欄位裝進 Cells（key-value）
                vmRow.Cells.Add(new FormDataCell { ColumnName = kv.Key, Value = kv.Value });
            }
            rows.Add(vmRow);
        }

        // 6. 若無資料，直接回傳（節省下方查詢）
        if (!rowIds.Any())
        {
            return new FormListDataViewModel { FormId = master.ID, Columns = columns, Rows = rows };
        }

        // 7. 查出所有資料列對應的下拉選答案（一次批次查）
        var dropdownAnswers = _con.Query<(string RowId, Guid FieldId, Guid OptionId)>(
            "/**/SELECT ROW_ID, FORM_FIELD_CONFIG_ID AS FieldId, FORM_FIELD_DROPDOWN_OPTIONS_ID AS OptionId " +
            "FROM FORM_FIELD_DROPDOWN_ANSWER WHERE ROW_ID IN @RowIds",
            new { RowIds = rowIds.Select(id => id.ToString()).ToList() }).ToList();

        // 8. 撈出所有 OptionId → OptionText 的對應表（只查一次）
        var optionIds = dropdownAnswers.Select(a => a.OptionId).Distinct().ToList();
        var optionTextMap = optionIds.Any()
            ? _con.Query<(Guid Id, string Text)>(
                "/**/SELECT ID, OPTION_TEXT AS Text FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE ID IN @Ids",
                new { Ids = optionIds })
                .ToDictionary(x => x.Id, x => x.Text)
            : new Dictionary<Guid, string>();

        // 9. 建立 RowId → (ConfigId → OptionId) 的對照表，加速查找
        var answerGroupMap = dropdownAnswers
            .GroupBy(a => a.RowId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.FieldId, x => x.OptionId),
                StringComparer.OrdinalIgnoreCase
            );

        // 10. 將所有下拉選欄位的值替換為對應顯示文字
        foreach (var row in rows)
        {
            // 找到這一列的所有 Dropdown 答案（如果沒有就略過）
            if (!answerGroupMap.TryGetValue(row.Id.ToString(), out var answers))
            {
                continue;
            }

            // 先把所有下拉欄位的答案 OptionId 存成查找表，
            // 再回頭把資料表裡原本的 id 值，一個一個查，
            // 如果是下拉欄位，就把它覆蓋成 OptionText（顯示用文字）。
            foreach (var (columnName, configId) in dropdownColumns)
            {
                // 取得該欄位的 OptionId
                if (answers.TryGetValue(configId, out var optionId) && optionTextMap.TryGetValue(optionId, out var optionText))
                {
                    // 找到該 Cell，直接用 OptionText 覆蓋原本值
                    var cell = row.Cells.FirstOrDefault(c => string.Equals(c.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
                    if (cell != null)
                    {
                        cell.Value = optionText;
                    }
                }
            }
        }

        // 11. 組成回傳 ViewModel
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

        // 2. 取得欄位設定
        // List<FormFieldInputViewModel> fields;
        // // if (master.SCHEMA_TYPE != (int)TableSchemaQueryType.All)
        // // {
        // //     fields = GetFields(master.ID);
        // //     return new FormSubmissionViewModel
        // //     {
        // //         FormId = master.ID,
        // //         RowId = fromId,
        // //         FormName = master.FORM_NAME,
        // //         Fields = fields
        // //     };
        // // }

        if (master.BASE_TABLE_ID is null || master.VIEW_TABLE_ID is null)
            throw new InvalidOperationException("主表與檢視表 ID 不完整");

        var baseFields = GetFields(master.BASE_TABLE_ID.Value, TableSchemaQueryType.OnlyTable, master.BASE_TABLE_NAME);
        var viewFields = GetFields(master.VIEW_TABLE_ID.Value, TableSchemaQueryType.OnlyView, master.VIEW_TABLE_NAME);

        var baseMap = baseFields.ToDictionary(
            f => (f.COLUMN_NAME.ToLower(), f.DATA_TYPE.ToLower()),
            f => f
        );

        var merged = new List<FormFieldInputViewModel>();
        
        // 組合起來，判斷誰可以編輯(主檔欄位的可以編輯)
        foreach (var viewField in viewFields)
        {
            bool isEditable = IsEditableFromBaseTable(viewField, baseMap, master.BASE_TABLE_NAME);
            viewField.IS_EDITABLE = isEditable;
            viewField.SOURCE = isEditable ? TableSchemaQueryType.OnlyTable : TableSchemaQueryType.OnlyView;
            merged.Add(viewField);
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

            foreach (var field in merged)
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
            Fields = merged
        };
    }
    
    /// <summary>
    /// 判斷 View 欄位是否來自主表，並且該欄位設定為可編輯
    /// </summary>
    private static bool IsEditableFromBaseTable(
        FormFieldInputViewModel viewField,
        Dictionary<(string, string), FormFieldInputViewModel> baseMap,
        string baseTableName)
    {
        var key = (viewField.COLUMN_NAME.ToLower(), viewField.DATA_TYPE.ToLower());

        return string.Equals(viewField.SOURCE_TABLE, baseTableName, StringComparison.OrdinalIgnoreCase)
               && baseMap.TryGetValue(key, out var baseField)
               && baseField.IS_EDITABLE;
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
            "SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG"
        ).ToDictionary(c => c.Id);
        var sourceMap = GetViewColumnSources(master.VIEW_TABLE_NAME);
        var baseColumns = sourceMap
            .Where(kv => string.Equals(kv.Value, master.BASE_TABLE_NAME, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 3. 分類欄位答案（依照型態拆成兩組）
        // - normalFields: 一般型態（非下拉選）欄位，用於直接寫入主資料表
        // - dropdownAnswers: 下拉選單答案，需額外存到 FORM_FIELD_DROPDOWN_ANSWER 關聯表
        var normalFields = new List<(string Column, object? Value)>();
        var dropdownAnswers = new List<(Guid ConfigId, Guid OptionId)>();

        // 逐一處理前端傳來的欄位答案
        foreach (var field in input.InputFields)
        {
            // 欄位 ID 在設定中找不到或不屬於主表則跳過
            if (!configs.TryGetValue(field.FieldConfigId, out var cfg))
                continue;
            if (!baseColumns.Contains(cfg.Column))
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
            case "decimal":
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