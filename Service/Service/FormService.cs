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

    public FormSubmissionViewModel GetFormSubmission(Guid id, Guid? fromId = null)
    {
        // 1. 查 Master 設定
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id });
        if (master == null)
            throw new InvalidOperationException($"FORM_FIELD_Master {id} not found");

        // 2. 取得欄位設定
        List<FormFieldInputViewModel> fields;
        if (master.SCHEMA_TYPE != (int)TableSchemaQueryType.All)
        {
            fields = GetFields(master.ID);
            return new FormSubmissionViewModel
            {
                FormId = master.ID,
                RowId = fromId,
                FormName = master.FORM_NAME,
                Fields = fields
            };
        }

        if (master.BASE_TABLE_ID is null || master.VIEW_TABLE_ID is null)
            throw new InvalidOperationException("主表與檢視表 ID 不完整");

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
        
        if (!string.IsNullOrWhiteSpace(master.PRIMARY_KEY)
            && !string.IsNullOrWhiteSpace(master.VIEW_TABLE_NAME)
            && fromId != null)
        {
            var sql = $"SELECT * FROM [{master.VIEW_TABLE_NAME}] WHERE [{master.PRIMARY_KEY}] = @id";
            var dataRow = _con.QueryFirstOrDefault(sql, new { id = fromId });

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
            var dropdownId = dropdown?.ID ?? Guid.Empty;
            var options = optionMap.TryGetValue(dropdownId, out var opts) ? opts.ToList() : new List<FORM_FIELD_DROPDOWN_OPTIONS>();
            
            var finalOptions = isUseSql
                ? options.Where(x => !string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList()
                : options.Where(x => string.IsNullOrWhiteSpace(x.OPTION_TABLE)).ToList();
            
            return new FormFieldInputViewModel
            {
                FieldConfigId = field.ID,
                COLUMN_NAME = field.COLUMN_NAME,
                CONTROL_TYPE = field.CONTROL_TYPE,
                DefaultValue = field.DEFAULT_VALUE,
                IS_VISIBLE = field.IS_VISIBLE,
                IS_EDITABLE = field.IS_EDITABLE,
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

    public void SubmitForm(Guid formId, Guid? rowId, Dictionary<Guid, string> fields)
    {
        var master = _con.QueryFirstOrDefault<FORM_FIELD_Master>("SELECT * FROM FORM_FIELD_Master WHERE ID = @id", new { id = formId });
        if (master == null)
            throw new InvalidOperationException($"FORM_FIELD_Master {formId} not found");

        if (master.BASE_TABLE_ID == null || string.IsNullOrWhiteSpace(master.PRIMARY_KEY))
            throw new InvalidOperationException("BASE_TABLE_ID or PRIMARY_KEY not set");

        var configs = _con.Query<(Guid ID, string TABLE_NAME, string COLUMN_NAME, int CONTROL_TYPE)>(
            "SELECT ID, TABLE_NAME, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
            new { id = master.BASE_TABLE_ID });

        var map = configs.ToDictionary(c => c.ID, c => c);

        var parameters = new DynamicParameters();
        var assignments = new List<string>();
        var insertColumns = new List<string>();
        var insertValues = new List<string>();
        var dropdownValues = new List<(Guid ConfigId, Guid OptionId)>();
        int idx = 0;

        foreach (var kv in fields)
        {
            if (!map.TryGetValue(kv.Key, out var cfg))
                continue;

            if ((FormControlType)cfg.CONTROL_TYPE == FormControlType.Dropdown)
            {
                if (Guid.TryParse(kv.Value, out var optionId))
                {
                    dropdownValues.Add((cfg.ID, optionId));
                }
            }
            else
            {
                var paramName = $"p{idx}";
                if (rowId == null)
                {
                    insertColumns.Add($"[{cfg.COLUMN_NAME}]");
                    insertValues.Add($"@{paramName}");
                }
                else
                {
                    assignments.Add($"[{cfg.COLUMN_NAME}] = @{paramName}");
                }
                parameters.Add(paramName, kv.Value);
                idx++;
            }
        }

        Guid finalRowId = rowId ?? Guid.NewGuid();

        if (rowId == null)
        {
            insertColumns.Insert(0, $"[{master.PRIMARY_KEY}]");
            insertValues.Insert(0, "@rowId");
            parameters.Add("rowId", finalRowId);
            var sql = $"INSERT INTO [{master.BASE_TABLE_NAME}] ({string.Join(", ", insertColumns)}) VALUES ({string.Join(", ", insertValues)})";
            _con.Execute(sql, parameters);
        }
        else if (assignments.Count > 0)
        {
            parameters.Add("rowId", finalRowId);
            var sql = $"UPDATE [{master.BASE_TABLE_NAME}] SET {string.Join(", ", assignments)} WHERE [{master.PRIMARY_KEY}] = @rowId";
            _con.Execute(sql, parameters);
        }

        foreach (var dv in dropdownValues)
        {
            _con.Execute(Sql.UpsertDropdownAnswer, new { ConfigId = dv.ConfigId, RowId = finalRowId, OptionId = dv.OptionId });
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
