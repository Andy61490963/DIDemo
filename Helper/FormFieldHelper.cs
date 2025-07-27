using ClassLibrary;

namespace DynamicForm.Helper;

public static class FormFieldHelper
{
    private static readonly Dictionary<string, List<FormControlType>> ControlTypeWhitelistMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "datetime", new() { FormControlType.Date } },
        { "bit",      new() { FormControlType.Checkbox } },
        { "int",      new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { "decimal",  new() { FormControlType.Number, FormControlType.Text } },
        { "nvarchar", new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { "varchar",  new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { "default",  new() { FormControlType.Text, FormControlType.Textarea } }
    };

    private static readonly Dictionary<string, FormControlType> DefaultControlTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "datetime", FormControlType.Date },
        { "bit",      FormControlType.Checkbox },
        { "int",      FormControlType.Number },
        { "decimal",  FormControlType.Number },
        { "nvarchar", FormControlType.Text },
        { "varchar",  FormControlType.Text },
        { "default",  FormControlType.Text }
    };

    public static List<FormControlType> GetControlTypeWhitelist(string dataType)
    {
        return ControlTypeWhitelistMap.TryGetValue(dataType, out var list)
            ? list
            : ControlTypeWhitelistMap["default"];
    }

    public static int GetDefaultEditorWidth(string dataType)
    {
        return dataType switch
        {
            "nvarchar" => 200,
            "varchar"  => 200,
            "text"     => 300,
            "int" or "decimal" => 100,
            _ => 150
        };
    }

    public static FormControlType GetDefaultControlType(string dataType)
    {
        return DefaultControlTypeMap.TryGetValue(dataType, out var type)
            ? type
            : DefaultControlTypeMap["default"];
    }
}
