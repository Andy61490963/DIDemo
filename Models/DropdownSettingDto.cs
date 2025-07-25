using System.Collections.Generic;

namespace DynamicForm.Models;

public class DropdownSettingDto
{
    public bool IsUseSql { get; set; }
    public string DropdownSql { get; set; } = string.Empty;
    public List<FormFieldValidationRuleDropdownDto> Options { get; set; } = new();
}
