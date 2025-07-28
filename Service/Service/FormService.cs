using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormService : IFormService
{
    private readonly SqlConnection _con;
    
    public FormService(SqlConnection connection)
    {
        _con = connection;
    }
    
    public FormSubmissionViewModel GetFormSubmission(Guid ID)
    {
        var sql = @"
            SELECT FFM.FORM_NAME, FFC.* FROM FORM_FIELD_CONFIG FFC
            JOIN FORM_FIELD_Master FFM
            ON FFM.ID = FFC.FORM_FIELD_MASTER_ID
            WHERE FFM.ID = @ID 
            ORDER BY FIELD_ORDER;

            SELECT R.* 
            FROM FORM_FIELD_VALIDATION_RULE R
            JOIN FORM_FIELD_CONFIG C ON R.FIELD_CONFIG_ID = C.ID
            WHERE C.FORM_FIELD_MASTER_ID = @ID;

            SELECT D.*
            FROM FORM_FIELD_DROPDOWN D
            JOIN FORM_FIELD_CONFIG C ON D.FORM_FIELD_CONFIG_ID = C.ID
            WHERE C.FORM_FIELD_MASTER_ID = @ID;

            SELECT O.*
            FROM FORM_FIELD_DROPDOWN_OPTIONS O
            JOIN FORM_FIELD_DROPDOWN D ON O.FORM_FIELD_DROPDOWN_ID = D.ID
            JOIN FORM_FIELD_CONFIG C ON D.FORM_FIELD_CONFIG_ID = C.ID
            WHERE C.FORM_FIELD_MASTER_ID = @ID;
        ";

        using var multi = _con.QueryMultiple(sql, new { ID });

        var fieldConfigs = multi.Read<FormFieldConfigDto>().ToList();
        var validationRules = multi.Read<FormFieldValidationRuleDto>().ToList();
        var dropdownConfigs = multi.Read<FORM_FIELD_DROPDOWN>().ToList();
        var dropdownOptions = multi.Read<FORM_FIELD_DROPDOWN_OPTIONS>().ToList();

        // 映射表
        var ruleMap = validationRules
            .GroupBy(r => r.FIELD_CONFIG_ID)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FormFieldValidationRuleDto>)g.ToList());

        var dropdownConfigMap = dropdownConfigs
            .GroupBy(d => d.FORM_FIELD_CONFIG_ID)
            .ToDictionary(g => g.Key, g => g.First());

        var optionMap = dropdownOptions
            .GroupBy(o => o.FORM_FIELD_DROPDOWN_ID)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FORM_FIELD_DROPDOWN_OPTIONS>)g.ToList());

        // 組裝欄位
        var fieldViewModels = fieldConfigs.Select(field =>
        {
            dropdownConfigMap.TryGetValue(field.ID, out var dropdown);
            var isUseSql = dropdown?.ISUSESQL ?? false;

            var finalOptions = isUseSql && dropdown != null
                ? ExecuteDynamicDropdownSql(dropdown)
                : (optionMap.TryGetValue(dropdown?.ID ?? Guid.Empty, out var opts) ? opts.ToList() : new());

            return new FormFieldInputViewModel
            {
                FieldConfigId = field.ID,
                COLUMN_NAME = field.COLUMN_NAME,
                CONTROL_TYPE = field.CONTROL_TYPE,
                DefaultValue = field.DEFAULT_VALUE,
                IS_VISIBLE = field.IS_VISIBLE,
                IS_EDITABLE = field.IS_EDITABLE,
                COLUMN_SPAN = field.COLUMN_SPAN,
                IS_SECTION_START = field.IS_SECTION_START,
                ValidationRules = ruleMap.TryGetValue(field.ID, out var rules) ? rules.ToList() : new(),
                OptionList = finalOptions,
                ISUSESQL = isUseSql,
                DROPDOWNSQL = dropdown?.DROPDOWNSQL ?? ""
            };
        }).ToList();

        return new FormSubmissionViewModel
        {
            FormName = fieldConfigs.Select(x => x.FORM_NAME).First(),
            Fields = fieldViewModels
        };
    }


    private List<FORM_FIELD_DROPDOWN_OPTIONS> ExecuteDynamicDropdownSql(FORM_FIELD_DROPDOWN dropdown)
    {
        var finalOptions = new List<FORM_FIELD_DROPDOWN_OPTIONS>();

        try
        {
            var trimmedSql = dropdown.DROPDOWNSQL?.TrimStart();
            if (string.IsNullOrWhiteSpace(trimmedSql) || !trimmedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("只允許 SELECT 查詢");

            // 執行 SQL
            var rows = _con.Query(dropdown.DROPDOWNSQL);

            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object>)row;

                var values = dict.Values.Take(2).ToArray();
                var optionValue = values.ElementAtOrDefault(0)?.ToString() ?? string.Empty;
                var optionText  = values.ElementAtOrDefault(1)?.ToString() ?? string.Empty;

                finalOptions.Add(new FORM_FIELD_DROPDOWN_OPTIONS
                {
                    ID = Guid.NewGuid(),
                    FORM_FIELD_DROPDOWN_ID = dropdown.ID,
                    OPTION_VALUE = optionValue,
                    OPTION_TEXT = optionText,
                    OPTION_TABLE = string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Dropdown SQL 查詢失敗: {ex.Message}");
        }

        return finalOptions;
    }

}