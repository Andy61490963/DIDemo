using ClassLibrary;

namespace DynamicForm.Models;

public class FormFieldConfigDto
{
    public Guid ID { get; set; }

    public Guid FORM_FIELD_Master_ID { get; set; }

    public string FORM_NAME { get; set; } = string.Empty;

    public string TABLE_NAME { get; set; } = string.Empty;
    
    public string SOURCE_TABLE { get; set; }

    public string COLUMN_NAME { get; set; } = string.Empty;

    public FormControlType CONTROL_TYPE { get; set; }

    public string? DEFAULT_VALUE { get; set; }

    public bool IS_REQUIRED { get; set; }

    public bool IS_EDITABLE { get; set; }

    public int FIELD_ORDER { get; set; }
    
    public string DATA_TYPE { get; set; }
}