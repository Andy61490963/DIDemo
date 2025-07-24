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
    public int CONTROL_TYPE { get; set; } // input / select / textarea ...
    public string Label { get; set; }
    public string Placeholder { get; set; }
    public string HelpText { get; set; }
    public bool IS_VISIBLE { get; set; }
    public bool IS_EDITABLE { get; set; }
    public int COLUMN_SPAN { get; set; } = 12;
    public bool IS_SECTION_START { get; set; }
    public string? UserValue { get; set; }
    public List<string>? OptionList { get; set; } // for select
    public List<FormFieldValidationRuleDto> ValidationRules { get; set; }
}
