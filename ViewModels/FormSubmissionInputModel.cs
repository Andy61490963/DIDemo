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
