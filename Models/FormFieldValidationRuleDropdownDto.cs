using System;

namespace DynamicForm.Models;

public class FormFieldValidationRuleDropdownDto
{
    public Guid ID { get; set; }
    public Guid FORM_FIELD_VALIDATION_RULE_ID { get; set; }
    public string OPTION_TEXT { get; set; } = string.Empty;
}
