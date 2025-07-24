using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormDesignerService : IFormDesignerService
{
    private readonly SqlConnection _con;
    
    public FormDesignerService(SqlConnection connection)
    {
        _con = connection;
    }

    public List<FormFieldViewModel> GetFieldsByTableName(string tableName)
    {
        // 1. 查詢欄位定義
        var columns = GetTableSchema(tableName);
        if (!columns.Any()) return new();

        // 2. 查欄位設定與驗證/語系
        var configs = GetFieldConfigs(tableName);
        var requiredFieldIds = GetRequiredFieldIds();
        var langLookup = GetLangCodeLookup();

        // 3. 組成 ViewModel
        var result = new List<FormFieldViewModel>();

        foreach (var col in columns)
        {
            var columnName = col.COLUMN_NAME;
            var dataType = col.DATA_TYPE;

            // config Dictionary<string, FormFieldConfigDto>
            if (configs.TryGetValue(columnName, out var config))
            {
                var fieldId = config.ID;

                result.Add(new FormFieldViewModel
                {
                    ID = fieldId,
                    TableName = tableName,
                    COLUMN_NAME = columnName,
                    DATA_TYPE = dataType,
                    CONTROL_TYPE = config.CONTROL_TYPE,
                    CONTROL_TYPE_WHITELIST = GetControlTypeWhitelist(dataType),
                    IS_VISIBLE = config.IS_VISIBLE ?? true,
                    IS_EDITABLE = config.IS_EDITABLE ?? true,
                    IS_VALIDATION_RULE = requiredFieldIds.Contains(fieldId),
                    LANG_CODES = langLookup.GetValueOrDefault(fieldId) ?? new(),
                    EDITOR_WIDTH = config.COLUMN_SPAN ?? 100
                });
            }
            else
            {
                result.Add(new FormFieldViewModel
                {
                    ID = Guid.NewGuid(),
                    TableName = tableName,
                    COLUMN_NAME = columnName,
                    DATA_TYPE = dataType,
                    CONTROL_TYPE_WHITELIST = GetControlTypeWhitelist(dataType),
                    IS_VISIBLE = true,
                    IS_EDITABLE = true,
                    IS_VALIDATION_RULE = false,
                    LANG_CODES = new(),
                    EDITOR_WIDTH = GetDefaultEditorWidth(dataType)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 儲存基本欄位設定(FORM_FIELD_CONFIG)
    /// </summary>
    /// <param name="model"></param>
    public void UpsertField(FormFieldViewModel model)
    {
        const string sql = @"
    MERGE FORM_FIELD_CONFIG AS target
    USING (VALUES (@ID, @TABLE_NAME, @COLUMN_NAME)) AS src(ID, TABLE_NAME, COLUMN_NAME)
        ON target.ID = src.ID
    WHEN MATCHED THEN
        UPDATE SET
            CONTROL_TYPE = @CONTROL_TYPE,
            IS_VISIBLE   = @IS_VISIBLE,
            IS_EDITABLE  = @IS_EDITABLE,
            COLUMN_SPAN  = @COLUMN_SPAN,
            DEFAULT_VALUE= @DEFAULT_VALUE,
            EDIT_TIME    = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT (ID, TABLE_NAME, COLUMN_NAME, CONTROL_TYPE, IS_VISIBLE, IS_EDITABLE, COLUMN_SPAN, DEFAULT_VALUE)
        VALUES (NEWID(), @TABLE_NAME, @COLUMN_NAME, @CONTROL_TYPE, @IS_VISIBLE, @IS_EDITABLE, @COLUMN_SPAN, @DEFAULT_VALUE);";

        var affected = _con.Execute(sql, new {
            model.ID,
            TABLE_NAME   = model.TableName,
            model.COLUMN_NAME,
            model.CONTROL_TYPE,
            model.IS_VISIBLE,
            model.IS_EDITABLE,
            COLUMN_SPAN  = model.EDITOR_WIDTH,
            model.DEFAULT_VALUE
        });

        if (affected == 0)
            throw new InvalidOperationException("Upsert 失敗，找不到目標欄位。");
    }


    public bool CheckFieldExists(Guid fieldId)
    {
        const string sql = @"SELECT COUNT(1) FROM FORM_FIELD_CONFIG WHERE ID = @fieldId";
        var exists = _con.ExecuteScalar<int>(sql, new { fieldId }) > 0;
        return exists;
    }
    
    public List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId)
    {
        const string sql = @"
        SELECT *
        FROM FORM_FIELD_VALIDATION_RULE
        WHERE FIELD_CONFIG_ID = @fieldId
        ORDER BY VALIDATION_ORDER";

        return _con.Query<FormFieldValidationRuleDto>(sql, new { fieldId }).ToList();
    }

    public bool HasValidationRules(Guid fieldId)
    {
        const string sql = @"SELECT COUNT(1) FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID = @fieldId";
        return _con.ExecuteScalar<int>(sql, new { fieldId }) > 0;
    }

    public void InsertValidationRule(FormFieldValidationRuleDto model)
    {
        const string sql = @"
        INSERT INTO FORM_FIELD_VALIDATION_RULE (
            ID, FIELD_CONFIG_ID, VALIDATION_TYPE, VALIDATION_VALUE, 
            MESSAGE_ZH, MESSAGE_EN, VALIDATION_ORDER
        ) VALUES (
            @ID, @FIELD_CONFIG_ID, @VALIDATION_TYPE, @VALIDATION_VALUE, 
            @MESSAGE_ZH, @MESSAGE_EN, @VALIDATION_ORDER
        )";
        _con.Execute(sql, model);
    }

    public int GetNextValidationOrder(Guid fieldId)
    {
        const string sql = @"SELECT ISNULL(MAX(VALIDATION_ORDER), 0) + 1 FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID = @fieldId";
        return _con.ExecuteScalar<int>(sql, new { fieldId });
    }

    public FormControlType GetControlTypeByFieldId(Guid fieldId)
    {
        const string sql = @"SELECT CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE ID = @fieldId";
        var controlTypeValue = _con.ExecuteScalar<int?>(sql, new { fieldId });

        if (!controlTypeValue.HasValue)
            throw new InvalidOperationException($"Field configuration not found for {fieldId}");

        return (FormControlType)controlTypeValue.Value;
    }
    
    public bool SaveValidationRule(FormFieldValidationRuleDto rule)
    {
        const string sql = @"
        UPDATE FORM_FIELD_VALIDATION_RULE SET
            VALIDATION_TYPE = @VALIDATION_TYPE,
            VALIDATION_VALUE = @VALIDATION_VALUE,
            MESSAGE_ZH = @MESSAGE_ZH,
            MESSAGE_EN = @MESSAGE_EN,
            VALIDATION_ORDER = @VALIDATION_ORDER,
            EDIT_TIME = GETDATE()
        WHERE ID = @ID";

        try
        {
            int affectedRows = _con.Execute(sql, rule);
            return affectedRows > 0;
        }
        catch (Exception ex)
        {
            // TODO: add logging framework to record ex
            return false;
        }
    }

    public bool DeleteValidationRule(Guid id)
    {
        const string sql = @"DELETE FROM FORM_FIELD_VALIDATION_RULE WHERE ID = @id";
        try
        {
            int affectedRows = _con.Execute(sql, new { id });
            return affectedRows > 0;
        }
        catch (Exception)
        {
            // TODO: add logging framework to record the failure
            return false;
        }
    }
    
    private List<DbColumnInfo> GetTableSchema(string tableName)
    {
        const string sql = @"SELECT COLUMN_NAME, DATA_TYPE, ORDINAL_POSITION FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName ORDER BY ORDINAL_POSITION";
        return _con.Query<DbColumnInfo>(sql, new { TableName = tableName }).ToList();
    }

    private Dictionary<string, FormFieldConfigDto> GetFieldConfigs(string tableName)
    {
        const string sql = @"SELECT * FROM FORM_FIELD_CONFIG WHERE TABLE_NAME = @TableName";
        return _con.Query<FormFieldConfigDto>(sql, new { TableName = tableName })
                   .ToDictionary(x => x.COLUMN_NAME);
    }

    private HashSet<Guid> GetRequiredFieldIds()
    {
        const string sql = @"SELECT FIELD_CONFIG_ID FROM FORM_FIELD_VALIDATION_RULE";
        return _con.Query<Guid>(sql).ToHashSet();
    }

    private Dictionary<Guid, List<string>> GetLangCodeLookup()
    {
        const string sql = @"SELECT FIELD_CONFIG_ID, LANG_CODE FROM FORM_FIELD_LANG";
        return _con.Query(sql)
                   .GroupBy(x => (Guid)x.FIELD_CONFIG_ID)
                   .ToDictionary(g => g.Key, g => g.Select(x => (string)x.LANG_CODE).ToList());
    }
    
    // 根據資料型別給預設控制元件類型
    private static readonly Dictionary<string, List<FormControlType>> ControlTypeWhitelistMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "datetime", new() { FormControlType.Date } },
        { "bit", new() { FormControlType.Checkbox } },
        { "int", new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { "decimal", new() { FormControlType.Number, FormControlType.Text } },
        { "nvarchar", new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { "varchar", new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { "default", new() { FormControlType.Text, FormControlType.Textarea } }
    };


    private List<FormControlType> GetControlTypeWhitelist(string dataType)
    {
        var res = ControlTypeWhitelistMap.TryGetValue(dataType, out var list)
            ? list
            : ControlTypeWhitelistMap["default"];
        return res;
    }
    
    // 根據資料型別給預設欄位寬度（可調整）
    private int GetDefaultEditorWidth(string dataType)
    {
        var res = dataType switch
        {
            "nvarchar" => 200,
            "varchar" => 200,
            "text" => 300,
            "int" or "decimal" => 100,
            _ => 150
        };
        return res;
    }
}