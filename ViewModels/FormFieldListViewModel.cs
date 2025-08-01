using ClassLibrary;

namespace DynamicForm.Models;

public class FormFieldListViewModel
{
    /// <summary>
    /// FORM_FIELD_Master
    /// </summary>
    public Guid ID { get; set; }
    public string TableName { get; set; } = string.Empty;

    public TableSchemaQueryType SchemaQueryType { get; set; }
    public List<FormFieldViewModel> Fields { get; set; } = new();
}

