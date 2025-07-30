using ClassLibrary;

namespace DynamicForm.Models;

public class FormSubmissionViewModel
{
    public Guid FormId { get; set; }
    public string? RowId { get; set; }
    public string FormName { get; set; }
    public List<FormFieldInputViewModel> Fields { get; set; }
}

public class FormFieldInputViewModel
{
    public Guid FieldConfigId { get; set; }
    public string COLUMN_NAME { get; set; }
    public FormControlType CONTROL_TYPE { get; set; }
    public string? DefaultValue { get; set; }
    public bool IS_VISIBLE { get; set; }
    public bool IS_EDITABLE { get; set; }
    
    /// <summary>
    /// 欄位來源：主表或檢視表
    /// </summary>
    public TableSchemaQueryType SOURCE { get; set; }

    public List<FormFieldValidationRuleDto> ValidationRules { get; set; } = new();
    
    public bool ISUSESQL { get; set; }
    public string DROPDOWNSQL { get; set; } = string.Empty;
    public List<FORM_FIELD_DROPDOWN_OPTIONS> OptionList { get; set; } = new();
    
    public string DATA_TYPE { get; set; }
    /// <summary>
    /// 若欄位來自 View，可紀錄其實際來源表
    /// </summary>
    public string? SOURCE_TABLE { get; set; }
    
    public object? CurrentValue { get; set; }
}
