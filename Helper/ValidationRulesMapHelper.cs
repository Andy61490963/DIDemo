namespace ClassLibrary;

public static class ValidationRulesMap
{
    private static readonly Dictionary<FormControlType, ValidationType[]> _map = new()
    {
        { FormControlType.Text,     new[] { ValidationType.Required, ValidationType.Regex, ValidationType.Email, ValidationType.Number } },
        { FormControlType.Number,   new[] { ValidationType.Required, ValidationType.Min, ValidationType.Max, ValidationType.Number } },
        { FormControlType.Date,     new[] { ValidationType.Required, ValidationType.Min, ValidationType.Max } },
        { FormControlType.Checkbox, new[] { ValidationType.Required } },
        { FormControlType.Textarea, new[] { ValidationType.Required, ValidationType.Regex, ValidationType.Email, ValidationType.Number } },
        { FormControlType.Dropdown, new[] { ValidationType.Required } },
    };

    public static ValidationType[] GetValidations(FormControlType controlType)
    {
        return _map.TryGetValue(controlType, out var types) ? types : Array.Empty<ValidationType>();
    }
}
