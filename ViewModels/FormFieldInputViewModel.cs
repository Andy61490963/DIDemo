using ClassLibrary;
using DynamicForm.Models;

namespace DynamicForm.ViewModels;

public class FormFieldInputViewModel
{
    public Guid FieldConfigId { get; set; }
    public string Column { get; set; } = string.Empty;
    public string DATA_TYPE { get; set; }
    public FormControlType CONTROL_TYPE { get; set; }
    public string? DefaultValue { get; set; }
    public bool IS_REQUIRED { get; set; }
    public bool IS_EDITABLE { get; set; }

    public List<FormFieldValidationRuleDto> ValidationRules { get; set; } = new();

    public bool ISUSESQL { get; set; }
    public string DROPDOWNSQL { get; set; } = string.Empty;
    public List<FORM_FIELD_DROPDOWN_OPTIONS> OptionList { get; set; } = new();

    /// <summary>
    /// 查詢元件類型，決定搜尋欄位的呈現方式。
    /// </summary>
    public QueryConditionType QUERY_CONDITION_TYPE { get; set; }

    /// <summary>
    /// 查詢元件若為下拉選單，使用此 SQL 取得選項資料。
    /// </summary>
    public string QUERY_CONDITION_SQL { get; set; } = string.Empty;

    /// <summary>
    /// 查詢條件下拉選單的選項集合。
    /// </summary>
    public List<Option> QUERY_OPTIONS { get; set; } = new();

    /// <summary>
    /// 若欄位來自 View，可紀錄其實際來源表
    /// </summary>
    public TableSchemaQueryType? SOURCE_TABLE { get; set; }

    public object? CurrentValue { get; set; }
}

