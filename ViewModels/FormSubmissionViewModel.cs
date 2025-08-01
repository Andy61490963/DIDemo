using DynamicForm.Models;

namespace DynamicForm.ViewModels;

public class FormSubmissionViewModel
{
    public Guid FormId { get; set; }
    public string? RowId { get; set; }
    public string? TargetTableToUpsert { get; set; }
    public string FormName { get; set; }
    public List<FormFieldInputViewModel> Fields { get; set; }
}

