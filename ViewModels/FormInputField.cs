namespace DynamicForm.Models;

/// <summary>
/// Represent single field input for submission.
/// </summary>
public class FormInputField
{
    public Guid FieldConfigId { get; set; }
    public string? Value { get; set; }
    public string? Column { get; set; }

    public string? SOURCE_TABLE { get; set; }
}

