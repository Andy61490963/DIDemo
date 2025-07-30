using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DynamicForm.Service.Service;

public class FormDesignerService : IFormDesignerService
{
    private readonly SqlConnection _con;
    private readonly IConfiguration _configuration;
    
    public FormDesignerService(SqlConnection connection, IConfiguration configuration)
    {
        _con = connection;
        _configuration = configuration;
        _excludeColumns = _configuration.GetSection("DropdownSqlSettings:ExcludeColumns").Get<List<string>>() ?? new();
    }

    private readonly List<string> _excludeColumns;
    
    #region Public API
    public FormDesignerIndexViewModel GetFormDesignerIndexViewModel(Guid? id)
    {
        var master = GetFormMaster(id) ?? new();

        var result = new FormDesignerIndexViewModel
        {
            FormHeader = master,
            BaseFields = null!,
            ViewFields = null!,
            FieldSetting = null!
        };

        // 主表欄位
        var baseFields = GetFieldsByTableName(master.BASE_TABLE_NAME, TableSchemaQueryType.OnlyTable);
        baseFields.ID = master.ID;
        baseFields.type = TableSchemaQueryType.OnlyTable;
        result.BaseFields = baseFields;

        // View 欄位
        var viewFields = GetFieldsByTableName(master.VIEW_TABLE_NAME, TableSchemaQueryType.OnlyView);
        viewFields.ID = master.ID;
        viewFields.type = TableSchemaQueryType.OnlyView;
        result.ViewFields = viewFields;

        return result;
    }
    
    public Guid GetOrCreateFormMasterId(FORM_FIELD_Master model)
    {
        var sql = @"SELECT ID FROM FORM_FIELD_Master WHERE ID = @id";
        var res = _con.QueryFirstOrDefault<Guid?>(sql, new { id = model.ID });

        if (res.HasValue)
            return res.Value;

        var insertId = model.ID == Guid.Empty ? Guid.NewGuid() : model.ID;
        _con.Execute(@"
        INSERT INTO FORM_FIELD_Master (ID, FORM_NAME, STATUS, SCHEMA_TYPE)
        VALUES (@ID, @FORM_NAME, @STATUS, @SCHEMA_TYPE)", new
        {
            ID = insertId,
            model.FORM_NAME,
            model.STATUS,
            model.SCHEMA_TYPE
        });

        return insertId;
    }

    /// <summary>
    /// 根據資料表名稱，取得所有欄位資訊並合併 欄位設定、驗證、語系資訊。
    /// </summary>
    /// <param name="tableName">使用者輸入的表名稱</param>
    /// <returns>回傳多筆 FormFieldViewModel</returns>
    public FormFieldListViewModel GetFieldsByTableName(string tableName, TableSchemaQueryType schemaType)
    {
        var columns = GetTableSchema(tableName, schemaType);
        if (columns.Count == 0) return new();

        var configs= GetFieldConfigs(tableName);
        var requiredFieldIds= GetRequiredFieldIds();
        
        var res = columns.Select(col =>
        {
            var hasConfig = configs.TryGetValue(col.COLUMN_NAME, out var cfg);
            var fieldId   = hasConfig ? cfg!.ID : Guid.NewGuid();
            var dataType  = col.DATA_TYPE;

            return new FormFieldViewModel
            {
                ID                     = fieldId,
                FORM_FIELD_Master_ID   = cfg?.FORM_FIELD_Master_ID ?? Guid.Empty,
                TableName              = tableName,
                COLUMN_NAME            = col.COLUMN_NAME,
                SOURCE_TABLE           = col.SOURCE_TABLE,
                DATA_TYPE              = dataType,
                CONTROL_TYPE           = cfg?.CONTROL_TYPE,
                CONTROL_TYPE_WHITELIST = FormFieldHelper.GetControlTypeWhitelist(dataType),
                IS_REQUIRED             = cfg?.IS_REQUIRED  ?? true,
                IS_VISIBLE             = cfg?.IS_VISIBLE  ?? true,
                IS_EDITABLE            = cfg?.IS_EDITABLE ?? true,
                IS_VALIDATION_RULE     = requiredFieldIds.Contains(fieldId),
                //EDITOR_WIDTH           = cfg?.COLUMN_SPAN ?? FormFieldHelper.GetDefaultEditorWidth(dataType),
                DEFAULT_VALUE          = cfg?.DEFAULT_VALUE ??  string.Empty,
                SchemaType             = schemaType
            };
        })
        // 用設定檔過濾
        .Where(f => !_excludeColumns.Any(ex => 
            f.COLUMN_NAME.Contains(ex, StringComparison.OrdinalIgnoreCase)))
        .ToList();
        
        var masterId = configs.Values.FirstOrDefault()?.FORM_FIELD_Master_ID ?? Guid.Empty;

        var result = new FormFieldListViewModel
        {
            ID = masterId,
            TableName = tableName,
            Fields = res,
            type = schemaType
        };

        return result;
    }

    /// <summary>
    /// 搜尋表格時，如設定檔不存在則先寫入預設欄位設定。
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <returns>包含欄位設定的 ViewModel</returns>
    public FormFieldListViewModel EnsureFieldsSaved(string tableName, TableSchemaQueryType schemaType)
    {
        var columns = GetTableSchema(tableName, schemaType);
        if (columns.Count == 0) return new();

        FORM_FIELD_Master model = new FORM_FIELD_Master
        {
            FORM_NAME = tableName,
            STATUS = (int)TableStatusType.Draft,
            SCHEMA_TYPE = schemaType
        };
        var configs = GetFieldConfigs(tableName);
        var masterId = configs.Values.FirstOrDefault()?.FORM_FIELD_Master_ID
                       ?? GetOrCreateFormMasterId(model);

        foreach (var col in columns)
        {
            if (!configs.ContainsKey(col.COLUMN_NAME))
            {
                var vm = new FormFieldViewModel
                {
                    ID = Guid.NewGuid(),
                    FORM_FIELD_Master_ID = masterId,
                    TableName = tableName,
                    COLUMN_NAME = col.COLUMN_NAME,
                    SOURCE_TABLE = col.SOURCE_TABLE,
                    DATA_TYPE = col.DATA_TYPE,
                    // CONTROL_TYPE = FormFieldHelper.GetDefaultControlType(col.DATA_TYPE),
                    CONTROL_TYPE = FormControlType.Text,
                    IS_REQUIRED = true,
                    IS_VISIBLE = true,
                    IS_EDITABLE = true,
                    EDITOR_WIDTH = FormFieldHelper.GetDefaultEditorWidth(col.DATA_TYPE),
                    DEFAULT_VALUE = string.Empty,
                    SchemaType = schemaType
                };

                UpsertField(vm, masterId);
            }
        }

        var result = GetFieldsByTableName(tableName, schemaType);
        result.type = schemaType;
        return result;
    }

    /// <summary>
    /// 新增或更新欄位設定，若已存在則更新，否則新增。
    /// </summary>
    /// <param name="model">表單欄位的 ViewModel</param>
    public void UpsertField(FormFieldViewModel model, Guid formMasterId)
    {
        var controlType = model.CONTROL_TYPE ?? FormFieldHelper.GetDefaultControlType(model.DATA_TYPE);

        var param = new
        {
            ID = model.ID == Guid.Empty ? Guid.NewGuid() : model.ID,
            FORM_FIELD_Master_ID = formMasterId,
            TABLE_NAME = model.TableName,
            model.COLUMN_NAME,
            CONTROL_TYPE = controlType,
            model.IS_REQUIRED,
            model.IS_VISIBLE,
            model.IS_EDITABLE,
            model.DEFAULT_VALUE
        };

        var affected = _con.Execute(Sql.UpsertField, param);

        if (affected == 0)
        {
            throw new InvalidOperationException($"Upsert 失敗：{model.COLUMN_NAME} 無法新增或更新");
        }
    }

    /// <summary>
    /// 檢查指定 FORM_FIELD_CONFIG ID 是否已存在於設定資料表中。
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
    /// <returns>若存在則為 true，否則為 false</returns>
    public bool CheckFieldExists(Guid fieldId)
    {
        var res = _con.ExecuteScalar<int>(Sql.CheckFieldExists, new { fieldId }) > 0;
        return res;
    }

    /// <summary>
    /// 取得 FORM_FIELD_VALIDATION_RULE 的所有驗證規則（包含順序與錯誤訊息）。
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
    /// <returns>回傳驗證規則清單</returns>
    public List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId)
    {
        var sql = Sql.ValidationRuleSelect + " WHERE FIELD_CONFIG_ID = @fieldId ORDER BY VALIDATION_ORDER";
        return _con.Query<FormFieldValidationRuleDto>(sql, new { fieldId }).ToList();
    }

    /// <summary>
    /// 判斷欄位是否已設定任何驗證規則。
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
    /// <returns>若有規則則回傳 true</returns>
    public bool HasValidationRules(Guid fieldId)
    {
        var res = _con.ExecuteScalar<int>(Sql.CountValidationRules, new { fieldId }) > 0;
        return res;
    }

    /// <summary>
    /// 新增空的驗證規則
    /// </summary>
    /// <param name="fieldConfigId"></param>
    /// <returns></returns>
    public FormFieldValidationRuleDto CreateEmptyValidationRule(Guid fieldConfigId)
    {
        return new FormFieldValidationRuleDto
        {
            ID = Guid.NewGuid(),
            FIELD_CONFIG_ID = fieldConfigId,
            VALIDATION_VALUE = "",
            MESSAGE_ZH = "",
            MESSAGE_EN = "",
            VALIDATION_ORDER = GetNextValidationOrder(fieldConfigId)
        };
    }

    /// <summary>
    /// 新增一筆欄位驗證規則。
    /// </summary>
    /// <param name="model">驗證規則 DTO</param>
    public void InsertValidationRule(FormFieldValidationRuleDto model)
    {
        _con.Execute(Sql.InsertValidationRule, model);
    }

    /// <summary>
    /// 取得該欄位的下一個驗證順序編號（遞增）。
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
    /// <returns>回傳下一個排序值</returns>
    public int GetNextValidationOrder(Guid fieldId)
    {
        var res = _con.ExecuteScalar<int>(Sql.GetNextValidationOrder, new { fieldId });
        return res;
    }
    
    /// <summary>
    /// 根據欄位 ID 取得該欄位的控制類型（FormControlType Enum）。
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
    /// <returns>回傳控制類型 Enum</returns>
    public FormControlType GetControlTypeByFieldId(Guid fieldId)
    {
        var value = _con.ExecuteScalar<int?>(Sql.GetControlTypeByFieldId, new { fieldId }) ?? 0;
        return (FormControlType)value;
    }

    /// <summary>
    /// 儲存（更新）驗證規則。
    /// </summary>
    /// <param name="rule">要更新的驗證規則 DTO</param>
    /// <returns>更新成功則回傳 true</returns>
    public bool SaveValidationRule(FormFieldValidationRuleDto rule)
    {
       var res =_con.Execute(Sql.UpdateValidationRule, rule) > 0;
       return res;
    }

    /// <summary>
    /// 刪除一筆驗證規則。
    /// </summary>
    /// <param name="id">驗證規則的唯一識別碼</param>
    /// <returns>刪除成功則回傳 true</returns>
    public bool DeleteValidationRule(Guid id)
    {
        var res = _con.Execute(Sql.DeleteValidationRule, new { id }) > 0;
        return res;
    }

    public void EnsureDropdownCreated(Guid fieldId)
    {
        _con.Execute(Sql.EnsureDropdownExists, new { fieldId });
    }
    
    public DropDownViewModel GetDropdownSetting(Guid fieldId)
    {
        var dropDown = _con.QueryFirstOrDefault<DropDownViewModel>(Sql.GetDropdownByFieldId, new { fieldId });

        if (dropDown == null)
        {
            return new DropDownViewModel();
        }
        var optionTexts = GetDropdownOptions(dropDown.ID);
        dropDown.OPTION_TEXT = optionTexts;

        return dropDown;
    }
    
    public List<FORM_FIELD_DROPDOWN_OPTIONS> GetDropdownOptions(Guid dropDownId)
    {
        var optionTexts = _con.Query<FORM_FIELD_DROPDOWN_OPTIONS>(Sql.GetOptionByDropdownId, new { dropDownId }).ToList();
        
        return optionTexts;
    }

    public Guid SaveDropdownOption(Guid? id, Guid dropdownId, string optionText, string optionValue, string? optionTable = null)
    {
        var param = new
        {
            Id = (id == Guid.Empty ? null : id),
            DropdownId = dropdownId,
            OptionText = optionText,
            OptionValue = optionValue,
            OptionTable = optionTable
        };

        // ExecuteScalar 直接拿回 OUTPUT 的 Guid
        return _con.ExecuteScalar<Guid>(Sql.UpsertDropdownOption, param);
    }

    public void DeleteDropdownOption(Guid optionId)
    {
        _con.Execute(Sql.DeleteDropdownOption, new { optionId });
    }
    
    public void SaveDropdownSql(Guid fieldId, string sql)
    {
        _con.Execute(Sql.UpsertDropdownSql, new { fieldId, sql });
    }
    
    public void SetDropdownMode(Guid dropdownId, bool isUseSql)
    {
        _con.Execute(Sql.SetDropdownMode, new { DropdownId = dropdownId, IsUseSql = isUseSql });
    }
    
    public ValidateSqlResultViewModel ValidateDropdownSql(string sql)
    {
        var result = new ValidateSqlResultViewModel();

        try
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                result.Success = false;
                result.Message = "SQL 不可為空。";
                return result;
            }

            if (Regex.IsMatch(sql, @"\b(insert|update|delete|drop|alter|truncate|exec|merge)\b", RegexOptions.IgnoreCase))
            {
                result.Success = false;
                result.Message = "僅允許查詢類 SQL。";
                return result;
            }

            var wasClosed = _con.State != System.Data.ConnectionState.Open;
            if (wasClosed) _con.Open();

            using var cmd = new SqlCommand(sql, _con);
            using var reader = cmd.ExecuteReader();

            var columns = reader.GetColumnSchema();
            if (columns.Count < 2)
            {
                result.Success = false;
                result.Message = "SQL 必須回傳至少兩個欄位。";
                return result;
            }

            // 檢查第一個欄位是否包含任一個 _excludeColumns 關鍵字
            if (!_excludeColumns.Any(ex =>
                    columns[0].ColumnName.Contains(ex, StringComparison.OrdinalIgnoreCase)))
            {
                result.Success = false;
                result.Message = $"第一個欄位必須包含任一關鍵字：{string.Join(", ", _excludeColumns)}";
                return result;
            }

            var rows = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[columns[i].ColumnName] = reader.GetValue(i);
                }
                rows.Add(row);
            }

            result.Success = true;
            result.RowCount = rows.Count;
            result.Rows = rows.Take(10).ToList(); // 最多回傳前 10 筆

            if (wasClosed) _con.Close();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    public ValidateSqlResultViewModel ImportDropdownOptionsFromSql(string sql, Guid dropdownId, string optionTable)
    {
        // 若未指定資料表名稱，嘗試從 SQL 中解析
        if (string.IsNullOrWhiteSpace(optionTable))
        {
            var match = Regex.Match(sql, @"from\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase);
            optionTable = match.Success ? match.Groups[1].Value : string.Empty;
        }

        var validation = ValidateDropdownSql(sql);
        if (!validation.Success)
            return validation;

        if (string.IsNullOrWhiteSpace(optionTable))
        {
            validation.Success = false;
            validation.Message = "無法解析來源表名稱";
            return validation;
        }

        var wasClosed = _con.State != System.Data.ConnectionState.Open;
        if (wasClosed) _con.Open();

        try
        {
            var rows = _con.Query(sql);
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object>)row;
                var optionValue = dict.TryGetValue("ID", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
                var optionText  = dict.TryGetValue("NAME", out var t) ? t?.ToString() ?? string.Empty : string.Empty;

                _con.Execute(Sql.InsertOptionIgnoreDuplicate, new
                {
                    DropdownId = dropdownId,
                    OptionTable = optionTable,
                    OptionValue = optionValue,
                    OptionText  = optionText
                });
            }
        }
        finally
        {
            if (wasClosed) _con.Close();
        }

        return validation;
    }

    public Guid SaveFormHeader(FORM_FIELD_Master model)
    {
        // 若不存在則產生新 ID
        if (model.ID == Guid.Empty)
        {
            model.ID = Guid.NewGuid();
        }

        var id = _con.ExecuteScalar<Guid>(Sql.UpsertFormMaster, model);
        return id;
    }

    public bool CheckFormMasterExists(string baseTableName, string viewTableName, Guid? excludeId = null)
    {
        var count = _con.ExecuteScalar<int>(Sql.CheckFormMasterExists,
            new { baseTableName, viewTableName, excludeId });
        return count > 0;
    }

    public List<FORM_FIELD_Master> GetFormMasters()
    {
        var statusList = new[] { TableStatusType.Active, TableStatusType.Disabled };
        return _con.Query<FORM_FIELD_Master>(Sql.FormMasterSelect, new{ STATUS = statusList }).ToList();
    }

    public FORM_FIELD_Master? GetFormMaster(Guid? id)
    {
        return _con.QueryFirstOrDefault<FORM_FIELD_Master>(Sql.FormMasterById, new { id });
    }

    public void DeleteFormMaster(Guid id)
    {
        _con.Execute(Sql.DeleteFormMaster, new { id });
    }


    #endregion

    #region Private Helpers

    /// <summary>
    /// 從 SQL Server 的 INFORMATION_SCHEMA 取得指定表的欄位結構。
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <returns>回傳欄位定義清單</returns>
    private List<DbColumnInfo> GetTableSchema(string tableName, TableSchemaQueryType type)
    {
        var sql = Sql.TableSchemaSelect;
        var columns = _con.Query<DbColumnInfo>(sql, new { TableName = tableName, Type = (int)type }).ToList();

        if (type == TableSchemaQueryType.OnlyView)
        {
            var srcMap = GetViewColumnSources(tableName);
            foreach (var col in columns)
            {
                if (srcMap.TryGetValue(col.COLUMN_NAME, out var src))
                    col.SOURCE_TABLE = src;
            }
        }
        else
        {
            foreach (var col in columns) col.SOURCE_TABLE = tableName;
        }

        return columns;
    }

    /// <summary>
    /// 從 FORM_FIELD_CONFIG 查出該表的欄位設定資訊，並組成 Dictionary。
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <returns>回傳以 COLUMN_NAME 為鍵的設定資料</returns>
    private Dictionary<string, FormFieldConfigDto> GetFieldConfigs(string tableName)
    {
        var sql = Sql.FieldConfigSelect + " WHERE TABLE_NAME = @TableName";
        var res = _con.Query<FormFieldConfigDto>(sql, new { TableName = tableName })
            .ToDictionary(x => x.COLUMN_NAME, StringComparer.OrdinalIgnoreCase);
        return res;
    }

    /// <summary>
    /// 查詢所有有設定驗證規則的欄位 ID 清單。
    /// </summary>
    /// <returns>回傳欄位 ID 的 HashSet</returns>
    private HashSet<Guid> GetRequiredFieldIds()
    {
        var res = _con.Query<Guid>(Sql.GetRequiredFieldIds).ToHashSet();
        return res;
    }

    /// <summary>
    /// 取得 View 欄位來源表資訊
    /// </summary>
    private Dictionary<string, string?> GetViewColumnSources(string viewName)
    {
        var sql = @"
DECLARE @vid INT = OBJECT_ID(@ViewName);
SELECT name AS COLUMN_NAME,
       source_table AS SOURCE_TABLE
FROM sys.dm_exec_describe_first_result_set_for_object(@vid, NULL);";

        var list = _con.Query<ViewColumnSource>(sql, new { ViewName = viewName });
        return list.ToDictionary(x => x.COLUMN_NAME, x => x.SOURCE_TABLE, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region SQL
    private static class Sql
    {
        public const string FieldConfigSelect = @"/**/
SELECT *
FROM FORM_FIELD_CONFIG";

        public const string ValidationRuleSelect = @"/**/
SELECT *
FROM FORM_FIELD_VALIDATION_RULE";

        public const string TableSchemaSelect = @"/**/
SELECT COLUMN_NAME, DATA_TYPE, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = @TableName
  AND (
      (@Type = 0 AND TABLE_NAME NOT LIKE 'V_%')
      OR (@Type = 1 AND TABLE_NAME LIKE 'V_%')
  )
ORDER BY ORDINAL_POSITION";

        public const string UpsertFormMaster = @"/**/
MERGE FORM_FIELD_Master AS target
USING (SELECT @ID AS ID) AS src
ON target.ID = src.ID
WHEN MATCHED THEN
    UPDATE SET
        FORM_NAME        = @FORM_NAME,
        BASE_TABLE_NAME  = @BASE_TABLE_NAME,
        VIEW_TABLE_NAME  = @VIEW_TABLE_NAME,
        PRIMARY_KEY      = @PRIMARY_KEY,
        BASE_TABLE_ID    = @BASE_TABLE_ID,
        VIEW_TABLE_ID    = @VIEW_TABLE_ID
WHEN NOT MATCHED THEN
    INSERT (
        ID, FORM_NAME, BASE_TABLE_NAME, VIEW_TABLE_NAME,
        PRIMARY_KEY, BASE_TABLE_ID, VIEW_TABLE_ID, STATUS, SCHEMA_TYPE)
    VALUES (
        @ID, @FORM_NAME, @BASE_TABLE_NAME, @VIEW_TABLE_NAME,
        @PRIMARY_KEY, @BASE_TABLE_ID, @VIEW_TABLE_ID, @STATUS, @SCHEMA_TYPE)
OUTPUT INSERTED.ID;";

        public const string CheckFormMasterExists = @"/**/
SELECT COUNT(1) FROM FORM_FIELD_Master
WHERE BASE_TABLE_NAME = @baseTableName
  AND VIEW_TABLE_NAME = @viewTableName
  AND (@excludeId IS NULL OR ID <> @excludeId)";
        
        public const string UpsertField = @"
MERGE FORM_FIELD_CONFIG AS target
USING (VALUES (@ID)) AS src(ID)
ON target.ID = src.ID
WHEN MATCHED THEN
    UPDATE SET
        CONTROL_TYPE   = @CONTROL_TYPE,
        IS_REQUIRED     = @IS_REQUIRED,
        IS_VISIBLE     = @IS_VISIBLE,
        IS_EDITABLE    = @IS_EDITABLE,
        DEFAULT_VALUE  = @DEFAULT_VALUE,
        EDIT_TIME      = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (
        ID, FORM_FIELD_Master_ID, TABLE_NAME, COLUMN_NAME,
        CONTROL_TYPE, IS_REQUIRED, IS_VISIBLE, IS_EDITABLE, DEFAULT_VALUE, CREATE_TIME
    )
    VALUES (
        @ID, @FORM_FIELD_Master_ID, @TABLE_NAME, @COLUMN_NAME,
        @CONTROL_TYPE, @IS_REQUIRED, @IS_VISIBLE, @IS_EDITABLE, @DEFAULT_VALUE, GETDATE()
    );";

        public const string CheckFieldExists         = @"/**/
SELECT COUNT(1) FROM FORM_FIELD_CONFIG WHERE ID = @fieldId";
        
        public const string CountValidationRules     = @"/**/
SELECT COUNT(1) FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID = @fieldId";
        
        public const string InsertValidationRule     = @"/**/
INSERT INTO FORM_FIELD_VALIDATION_RULE (
    ID, FIELD_CONFIG_ID, VALIDATION_TYPE, VALIDATION_VALUE,
    MESSAGE_ZH, MESSAGE_EN, VALIDATION_ORDER
) VALUES (
    @ID, @FIELD_CONFIG_ID, @VALIDATION_TYPE, @VALIDATION_VALUE,
    @MESSAGE_ZH, @MESSAGE_EN, @VALIDATION_ORDER
)";

        public const string GetNextValidationOrder   = @"/**/
SELECT ISNULL(MAX(VALIDATION_ORDER), 0) + 1 FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID = @fieldId";
        
        public const string GetControlTypeByFieldId  = @"/**/
SELECT CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE ID = @fieldId";
        
        public const string UpdateValidationRule     = @"/**/
UPDATE FORM_FIELD_VALIDATION_RULE SET
    VALIDATION_TYPE  = @VALIDATION_TYPE,
    VALIDATION_VALUE = @VALIDATION_VALUE,
    MESSAGE_ZH       = @MESSAGE_ZH,
    MESSAGE_EN       = @MESSAGE_EN,
    VALIDATION_ORDER = @VALIDATION_ORDER,
    EDIT_TIME        = GETDATE()
WHERE ID = @ID";
        public const string DeleteValidationRule     = @"/**/
DELETE FROM FORM_FIELD_VALIDATION_RULE WHERE ID = @id";

        public const string GetRequiredFieldIds      = @"/**/
SELECT FIELD_CONFIG_ID FROM FORM_FIELD_VALIDATION_RULE";

        public const string EnsureDropdownExists = @"
/* 僅在尚未存在時插入 dropdown 主檔 */
IF NOT EXISTS (
    SELECT 1 FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID = @fieldId
)
BEGIN
    INSERT INTO FORM_FIELD_DROPDOWN (ID, FORM_FIELD_CONFIG_ID, ISUSESQL)
    VALUES (NEWID(), @fieldId, 0)
END
";
        
        public const string GetDropdownByFieldId = @"/**/
SELECT * FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID = @fieldId";
        
        public const string GetOptionByDropdownId = @"/**/
SELECT * FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE FORM_FIELD_DROPDOWN_ID = @dropDownId AND OPTION_TABLE IS NULL --NULL的是使用者自訂
";

        public const string UpsertDropdownSql = @"/**/
MERGE FORM_FIELD_DROPDOWN AS target
USING (
    SELECT 
        @fieldId AS FORM_FIELD_CONFIG_ID,
        @sql AS DROPDOWNSQL
) AS source
ON target.FORM_FIELD_CONFIG_ID = source.FORM_FIELD_CONFIG_ID

WHEN MATCHED THEN
    UPDATE SET 
        target.DROPDOWNSQL = source.DROPDOWNSQL,
        target.ISUSESQL = 1

WHEN NOT MATCHED THEN
    INSERT (ID, FORM_FIELD_CONFIG_ID, DROPDOWNSQL, ISUSESQL)
    VALUES (NEWID(), source.FORM_FIELD_CONFIG_ID, source.DROPDOWNSQL, 1);
";
        
        public const string UpsertDropdownOption = @"/**/
MERGE dbo.FORM_FIELD_DROPDOWN_OPTIONS AS target
USING (
    SELECT
        @Id             AS ID,                 -- Guid (可能是空)
        @DropdownId     AS FORM_FIELD_DROPDOWN_ID,
        @OptionText     AS OPTION_TEXT,
        @OptionValue    AS OPTION_VALUE,
        @OptionTable    AS OPTION_TABLE
) AS source
ON target.ID = source.ID                     -- 只比對 PK
WHEN MATCHED THEN
    UPDATE SET
        OPTION_TEXT  = source.OPTION_TEXT,
        OPTION_VALUE = source.OPTION_VALUE,
        OPTION_TABLE = source.OPTION_TABLE
WHEN NOT MATCHED THEN
    INSERT (ID, FORM_FIELD_DROPDOWN_ID, OPTION_TEXT, OPTION_VALUE, OPTION_TABLE)
    VALUES (ISNULL(source.ID, NEWID()),       -- 若 Guid.Empty → 直接 NEWID()
            source.FORM_FIELD_DROPDOWN_ID,
            source.OPTION_TEXT,
            source.OPTION_VALUE,
            source.OPTION_TABLE)
OUTPUT INSERTED.ID;                          -- 把 ID 回傳給 Dapper
";

        public const string InsertOptionIgnoreDuplicate = @"/**/
MERGE dbo.FORM_FIELD_DROPDOWN_OPTIONS AS target
USING (
    SELECT
        @DropdownId  AS FORM_FIELD_DROPDOWN_ID,
        @OptionTable AS OPTION_TABLE,
        @OptionValue AS OPTION_VALUE,
        @OptionText  AS OPTION_TEXT
) AS src
ON target.FORM_FIELD_DROPDOWN_ID = src.FORM_FIELD_DROPDOWN_ID
   AND target.OPTION_TABLE = src.OPTION_TABLE
   AND target.OPTION_VALUE = src.OPTION_VALUE
WHEN NOT MATCHED THEN
    INSERT (ID, FORM_FIELD_DROPDOWN_ID, OPTION_TABLE, OPTION_VALUE, OPTION_TEXT)
    VALUES (NEWID(), src.FORM_FIELD_DROPDOWN_ID, src.OPTION_TABLE, src.OPTION_VALUE, src.OPTION_TEXT);
";

        public const string DeleteDropdownOption = @"/**/
DELETE FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE ID = @optionId;
";
        
        public const string SetDropdownMode = @"
UPDATE dbo.FORM_FIELD_DROPDOWN
SET ISUSESQL   = @IsUseSql
WHERE ID = @DropdownId;
";

        public const string FormMasterSelect = @"SELECT * FROM FORM_FIELD_Master WHERE STATUS IN @STATUS";
        public const string FormMasterById   = @"SELECT * FROM FORM_FIELD_Master WHERE ID = @id";
        public const string DeleteFormMaster = @"
DELETE FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE FORM_FIELD_DROPDOWN_ID IN (
    SELECT ID FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID IN (
        SELECT ID FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id
    )
);
DELETE FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID IN (
    SELECT ID FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id
);
DELETE FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID IN (
    SELECT ID FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id
);
DELETE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id;
DELETE FROM FORM_FIELD_Master WHERE ID = @id;
";

    }
    #endregion
}