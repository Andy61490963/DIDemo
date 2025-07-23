namespace DynamicForm.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class Option
{
    public string label { get; set; }
    public string value { get; set; }
}