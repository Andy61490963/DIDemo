using System;
using System.Collections.Generic;

namespace DynamicForm.Models;

/// <summary>
/// ViewModel for submitting form data from client.
/// </summary>
public class FormSubmissionInputModel
{
    public Guid FormId { get; set; }
    public string? RowId { get; set; }
    public string? TargetTableToUpsert { get; set; }
    public List<FormInputField> InputFields { get; set; } = new();
}
