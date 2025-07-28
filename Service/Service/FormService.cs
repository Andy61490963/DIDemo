using System.Text.RegularExpressions;
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

    public FormSubmissionViewModel GetFormSubmission(Guid id)
    {
        id = Guid.Parse("512DBC19-B37D-4E6E-9F56-5ACE6C745120");
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>("/**/SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id });
        if (master == null)
        {
            throw new InvalidOperationException($"FORM_FIELD_Master {id} not found");
        }
        
        if (master.SCHEMA_TYPE != (int)TableSchemaQueryType.All)
        {
            var fields = GetFields(master.ID);
            return new FormSubmissionViewModel
            {
                FormName = master.FORM_NAME,
                Fields = fields
            };
        }
        
        if (master.BASE_TABLE_ID is null || master.VIEW_TABLE_ID is null)
        {
            throw new InvalidOperationException("主表與檢視表 ID 不完整");
        }

        var baseFields = GetFields(master.BASE_TABLE_ID.Value);
        var viewFields = GetFields(master.VIEW_TABLE_ID.Value);

        var baseMap = baseFields.ToDictionary(f => f.COLUMN_NAME, f => f, StringComparer.OrdinalIgnoreCase);

        var merged = new List<FormFieldInputViewModel>();
        foreach (var viewField in viewFields)
        {
            if (baseMap.TryGetValue(viewField.COLUMN_NAME, out var baseField))
            {
                baseField.SOURCE = TableSchemaQueryType.OnlyTable;
                merged.Add(baseField);
            }
            else
            {
                viewField.IS_EDITABLE = false;
                viewField.SOURCE = TableSchemaQueryType.OnlyView;
                merged.Add(viewField);
            }
        }

        return new FormSubmissionViewModel
        {
            FormName = master.FORM_NAME,
            Fields = merged
        };
    }

    private List<FormFieldInputViewModel> GetFields(Guid masterId)
    {
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

        var ruleMap = validationRules.GroupBy(r => r.FIELD_CONFIG_ID)
                                     .ToDictionary(g => g.Key, g => (IReadOnlyList<FormFieldValidationRuleDto>)g.ToList());
        var dropdownConfigMap = dropdownConfigs.GroupBy(d => d.FORM_FIELD_CONFIG_ID)
                                               .ToDictionary(g => g.Key, g => g.First());
        var optionMap = dropdownOptions.GroupBy(o => o.FORM_FIELD_DROPDOWN_ID)
                                       .ToDictionary(g => g.Key, g => (IReadOnlyList<FORM_FIELD_DROPDOWN_OPTIONS>)g.ToList());

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
                DROPDOWNSQL = dropdown?.DROPDOWNSQL ?? string.Empty,
                SOURCE = TableSchemaQueryType.OnlyTable
            };
        }).ToList();

        return fieldViewModels;
    }

    private List<FORM_FIELD_DROPDOWN_OPTIONS> ExecuteDynamicDropdownSql(FORM_FIELD_DROPDOWN dropdown)
    {
        var finalOptions = new List<FORM_FIELD_DROPDOWN_OPTIONS>();
        try
        {
            var trimmedSql = dropdown.DROPDOWNSQL?.TrimStart();
            if (string.IsNullOrWhiteSpace(trimmedSql) || !trimmedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("只允許 SELECT 查詢");

            var rows = _con.Query(dropdown.DROPDOWNSQL);
            foreach (var row in rows)
            {
                var dict = (IDictionary<string, object>)row;
                var values = dict.Values.Take(2).ToArray();
                var optionValue = values.ElementAtOrDefault(0)?.ToString() ?? string.Empty;
                var optionText = values.ElementAtOrDefault(1)?.ToString() ?? string.Empty;
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
    
    /// <summary>
    /// 取得指定表單對應檢視表的所有資料
    /// </summary>
    public FormListDataViewModel GetFormList()
    {
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>("select * from FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE", new { TYPE = TableSchemaQueryType.All.ToInt() });
        if (master == null )
        {
            return new FormListDataViewModel();
        }

        var columnSql = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table";
        var columns = _con.Query<string>(columnSql, new { table = master.VIEW_TABLE_NAME }).ToList();

        var rows = new List<Dictionary<string, object?>>();
        var data = _con.Query($"SELECT * FROM {master.VIEW_TABLE_NAME}");
        foreach (IDictionary<string, object?> row in data)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var col in columns)
            {
                row.TryGetValue(col, out var value);
                dict[col] = value;
            }
            rows.Add(dict);
        }

        return new FormListDataViewModel
        {
            FormId = master.ID,
            Columns = columns,
            Rows = rows
        };
    }
}
