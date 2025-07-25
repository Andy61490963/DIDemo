using ClassLibrary;

namespace DynamicForm.Models;

public class FormFieldConfigDto
{
    public Guid ID { get; set; }

    public Guid FORM_FIELD_Master_ID { get; set; }

    public string FORM_NAME { get; set; } = string.Empty;

    public string TABLE_NAME { get; set; } = string.Empty;

    public string COLUMN_NAME { get; set; } = string.Empty;

    public FormControlType CONTROL_TYPE { get; set; }

    public string? DEFAULT_VALUE { get; set; }

    public bool IS_VISIBLE { get; set; }

    public bool IS_EDITABLE { get; set; }

    public int FIELD_ORDER { get; set; }

    public string? FIELD_GROUP { get; set; }

    public int? COLUMN_SPAN { get; set; }

    public bool IS_SECTION_START { get; set; }

    public string? CREATE_USER { get; set; }

    public DateTime? CREATE_TIME { get; set; }

    public string? EDIT_USER { get; set; }

    public DateTime? EDIT_TIME { get; set; }
}
