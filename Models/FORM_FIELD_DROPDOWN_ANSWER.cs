using System;

namespace DynamicForm.Models;

public class FORM_FIELD_DROPDOWN_ANSWER
{
    public Guid ID { get; set; }
    public Guid FORM_FIELD_CONFIG_ID { get; set; }
    public Guid ROW_ID { get; set; }
    public string OPTION_VALUE { get; set; } = string.Empty;
}

