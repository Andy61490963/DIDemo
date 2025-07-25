using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Helper;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormDesignerService : IFormDesignerService
{
    private readonly SqlConnection _con;
    
    public FormDesignerService(SqlConnection connection)
    {
        _con = connection;
    }

    #region Public API
    /// <summary>
    /// 根據資料表名稱，取得所有欄位資訊並合併 欄位設定、驗證、語系資訊。
    /// </summary>
    /// <param name="tableName">使用者輸入的表名稱</param>
    /// <returns>回傳多筆 FormFieldViewModel</returns>
    public FormFieldListViewModel GetFieldsByTableName(string tableName)
    {
        var columns = GetTableSchema(tableName);
        if (columns.Count == 0) return new();

        var configs= GetFieldConfigs(tableName);
        var requiredFieldIds= GetRequiredFieldIds();
        var langLookup= GetLangCodeLookup();

        var res = columns.Select(col =>
        {
            var hasConfig = configs.TryGetValue(col.COLUMN_NAME, out var cfg);
            var fieldId   = hasConfig ? cfg!.ID : Guid.NewGuid();
            var dataType  = col.DATA_TYPE;

            return new FormFieldViewModel
            {
                ID                     = fieldId,
                TableName              = tableName,
                COLUMN_NAME            = col.COLUMN_NAME,
                DATA_TYPE              = dataType,
                CONTROL_TYPE           = cfg?.CONTROL_TYPE,
                CONTROL_TYPE_WHITELIST = FormFieldHelper.GetControlTypeWhitelist(dataType),
                IS_VISIBLE             = cfg?.IS_VISIBLE  ?? true,
                IS_EDITABLE            = cfg?.IS_EDITABLE ?? true,
                IS_VALIDATION_RULE     = requiredFieldIds.Contains(fieldId),
                LANG_CODES             = langLookup.GetValueOrDefault(fieldId) ?? new(),
                EDITOR_WIDTH           = cfg?.COLUMN_SPAN ?? FormFieldHelper.GetDefaultEditorWidth(dataType),
                DEFAULT_VALUE          = cfg?.DEFAULT_VALUE ??  string.Empty
            };
        }).ToList();

        var result = new FormFieldListViewModel
        {
            TableName = tableName,
            Fields = res
        };
        
        return result;
    }

    /// <summary>
    /// 新增或更新欄位設定，若已存在則更新，否則新增。
    /// </summary>
    /// <param name="model">表單欄位的 ViewModel</param>
    public void UpsertField(FormFieldViewModel model)
    {
        var affected = _con.Execute(Sql.UpsertField, new
        {
            model.ID,
            TABLE_NAME = model.TableName,
            model.COLUMN_NAME,
            model.CONTROL_TYPE,
            model.IS_VISIBLE,
            model.IS_EDITABLE,
            COLUMN_SPAN   = model.EDITOR_WIDTH,
            model.DEFAULT_VALUE
        });

        if (affected == 0)
        {
            throw new InvalidOperationException($"Upsert 失敗，找不到目標欄位: {model.COLUMN_NAME}");
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
            VALIDATION_TYPE = "",
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
        var value = _con.ExecuteScalar<int?>(Sql.GetControlTypeByFieldId, new { fieldId })
                    ?? throw new InvalidOperationException($"Field not found: {fieldId}");
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

    public DropdownSettingDto GetDropdownSetting(Guid fieldId)
    {
        var rule = _con.QuerySingleOrDefault<FormFieldValidationRuleDto>(Sql.GetDropdownByFieldId, new { fieldId });
        if (rule == null)
        {
            return new DropdownSettingDto();
        }

        var options = _con.Query<FormFieldValidationRuleDropdownDto>(Sql.GetDropdownOptions, new { ruleId = rule.ID }).ToList();

        return new DropdownSettingDto
        {
            IsUseSql = rule.IS_USE_DROPDOWN_SQL,
            DropdownSql = rule.DROPDOWN_SQL ?? string.Empty,
            Options = options
        };
    }

    public void SaveDropdownSql(Guid fieldId, string sql)
    {
        var rule = _con.QuerySingleOrDefault<FormFieldValidationRuleDto>(Sql.GetDropdownByFieldId, new { fieldId });
        if (rule == null) return;

        _con.Execute(Sql.UpdateDropdownSql, new
        {
            id = rule.ID,
            sql,
        });

        _con.Execute(Sql.DeleteDropdownOptions, new { ruleId = rule.ID });
    }

    public void SaveDropdownOptions(Guid fieldId, IEnumerable<string> options)
    {
        var rule = _con.QuerySingleOrDefault<FormFieldValidationRuleDto>(Sql.GetDropdownByFieldId, new { fieldId });
        if (rule == null) return;

        _con.Execute(Sql.UpdateDropdownUseOptions, new { id = rule.ID });

        _con.Execute(Sql.DeleteDropdownOptions, new { ruleId = rule.ID });

        foreach (var text in options)
        {
            _con.Execute(Sql.InsertDropdownOption, new { ruleId = rule.ID, text });
        }
    }
    #endregion

    #region Private Helpers

    /// <summary>
    /// 從 SQL Server 的 INFORMATION_SCHEMA 取得指定表的欄位結構。
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <returns>回傳欄位定義清單</returns>
    private List<DbColumnInfo> GetTableSchema(string tableName)
    {
        var sql = Sql.TableSchemaSelect + " WHERE TABLE_NAME = @TableName ORDER BY ORDINAL_POSITION";
        return _con.Query<DbColumnInfo>(sql, new { TableName = tableName }).ToList();
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
    /// 取得欄位對應的語系資料（欄位翻譯等）。
    /// </summary>
    /// <returns>回傳 Dictionary：欄位 ID -> 語系清單</returns>
    private Dictionary<Guid, List<string>> GetLangCodeLookup()
    {
        var sql = Sql.LangCodeLookupSelect;
        var res = _con.Query(sql)
            .GroupBy(x => (Guid)x.FIELD_CONFIG_ID)
            .ToDictionary(g => g.Key, g => g.Select(x => (string)x.LANG_CODE).ToList());
        return res;
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

        public const string LangCodeLookupSelect = @"/**/
SELECT FIELD_CONFIG_ID, LANG_CODE
FROM FORM_FIELD_LANG";

        public const string TableSchemaSelect = @"/**/
SELECT COLUMN_NAME, DATA_TYPE, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS";
        
        public const string UpsertField = @"
MERGE FORM_FIELD_CONFIG AS target
USING (VALUES (@ID, @TABLE_NAME, @COLUMN_NAME)) AS src(ID, TABLE_NAME, COLUMN_NAME)
    ON target.ID = src.ID
WHEN MATCHED THEN
    UPDATE SET
        CONTROL_TYPE   = @CONTROL_TYPE,
        IS_VISIBLE     = @IS_VISIBLE,
        IS_EDITABLE    = @IS_EDITABLE,
        COLUMN_SPAN    = @COLUMN_SPAN,
        DEFAULT_VALUE  = @DEFAULT_VALUE,
        EDIT_TIME      = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (ID, TABLE_NAME, COLUMN_NAME, CONTROL_TYPE, IS_VISIBLE, IS_EDITABLE, COLUMN_SPAN, DEFAULT_VALUE)
    VALUES (NEWID(), @TABLE_NAME, @COLUMN_NAME, @CONTROL_TYPE, @IS_VISIBLE, @IS_EDITABLE, @COLUMN_SPAN, @DEFAULT_VALUE);";

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

        public const string GetDropdownByFieldId = @"/**/
SELECT * FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID = @fieldId";

        public const string GetDropdownOptions = @"/**/
SELECT * FROM FORM_FIELD_VALIDATION_RULE_DROPDOWN WHERE FORM_FIELD_VALIDATION_RULE_ID = @ruleId";

        public const string UpdateDropdownSql = @"/**/
UPDATE FORM_FIELD_VALIDATION_RULE
SET IS_USE_DROPDOWN_SQL = 1, DROPDOWN_SQL = @sql, EDIT_TIME = GETDATE()
WHERE ID = @id";

        public const string UpdateDropdownUseOptions = @"/**/
UPDATE FORM_FIELD_VALIDATION_RULE
SET IS_USE_DROPDOWN_SQL = 0, DROPDOWN_SQL = NULL, EDIT_TIME = GETDATE()
WHERE ID = @id";

        public const string InsertDropdownOption = @"/**/
INSERT INTO FORM_FIELD_VALIDATION_RULE_DROPDOWN
    (FORM_FIELD_VALIDATION_RULE_ID, OPTION_TEXT)
VALUES (@ruleId, @text)";

        public const string DeleteDropdownOptions = @"/**/
DELETE FROM FORM_FIELD_VALIDATION_RULE_DROPDOWN
WHERE FORM_FIELD_VALIDATION_RULE_ID = @ruleId";
        
    }
    #endregion
}