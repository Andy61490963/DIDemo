using ClassLibrary;

namespace DynamicForm.Models;

public class FormSubmissionViewModel
{
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
    public int? COLUMN_SPAN { get; set; }
    public bool IS_SECTION_START { get; set; }

    public List<FormFieldValidationRuleDto> ValidationRules { get; set; } = new();
    
    public bool ISUSESQL { get; set; }
    public string DROPDOWNSQL { get; set; } = string.Empty;
    public List<FORM_FIELD_DROPDOWN_OPTIONS> OptionList { get; set; } = new();
}
