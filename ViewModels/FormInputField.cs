namespace DynamicForm.ViewModels;

/// <summary>
/// Represent single field input for submission.
/// </summary>
public class FormInputField
{
    public Guid FieldConfigId { get; set; }
    public string? Value { get; set; }
    public string? Column { get; set; }
}

