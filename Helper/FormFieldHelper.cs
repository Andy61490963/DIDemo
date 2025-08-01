// using ClassLibrary;
//
// namespace DynamicForm.Helper;
//
// public static class FormFieldHelper
// {
//     private static readonly Dictionary<string, List<FormControlType>> ControlTypeWhitelistMap = new(StringComparer.OrdinalIgnoreCase)
//     {
//         { "datetime", new() { FormControlType.Date } },
//         { "bit",      new() { FormControlType.Checkbox } },
//         { "int",      new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
//         { "decimal",  new() { FormControlType.Number, FormControlType.Text } },
//         { "nvarchar", new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
//         { "varchar",  new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
//         { "default",  new() { FormControlType.Text, FormControlType.Textarea } }
//     };
//
//     private static readonly Dictionary<string, FormControlType> DefaultControlTypeMap = new(StringComparer.OrdinalIgnoreCase)
//     {
//         { "datetime", FormControlType.Date },
//         { "bit",      FormControlType.Checkbox },
//         { "int",      FormControlType.Number },
//         { "decimal",  FormControlType.Number },
//         { "nvarchar", FormControlType.Text },
//         { "varchar",  FormControlType.Text },
//         { "default",  FormControlType.Text }
//     };
//
//     public static List<FormControlType> GetControlTypeWhitelist(string dataType)
//     {
//         return ControlTypeWhitelistMap.TryGetValue(dataType, out var list)
//             ? list
//             : ControlTypeWhitelistMap["default"];
//     }
//
//     public static int GetDefaultEditorWidth(string dataType)
//     {
//         return dataType switch
//         {
//             "nvarchar" => 200,
//             "varchar"  => 200,
//             "text"     => 300,
//             "int" or "decimal" => 100,
//             _ => 150
//         };
//     }
//
//     public static FormControlType GetDefaultControlType(string dataType)
//     {
//         return DefaultControlTypeMap.TryGetValue(dataType, out var type)
//             ? type
//             : DefaultControlTypeMap["default"];
//     }
// }

using ClassLibrary;

namespace DynamicForm.Helper;

/// <summary>
/// 提供欄位對應的控制元件型別、預設寬度與邏輯的輔助方法。
/// </summary>
public static class FormFieldHelper
{
    /// <summary>
    /// 各 SQL 資料型別對應允許使用的控制元件清單。
    /// </summary>
    private static readonly Dictionary<SqlDataType, List<FormControlType>> ControlTypeWhitelistMap = new()
    {
        { SqlDataType.DateTime, new() { FormControlType.Date } },
        { SqlDataType.Bit,      new() { FormControlType.Checkbox } },
        { SqlDataType.Int,      new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { SqlDataType.Decimal,  new() { FormControlType.Number, FormControlType.Text } },
        { SqlDataType.NVarChar, new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { SqlDataType.VarChar,  new() { FormControlType.Number, FormControlType.Text, FormControlType.Dropdown } },
        { SqlDataType.Text,     new() { FormControlType.Textarea, FormControlType.Text } },
        { SqlDataType.Unknown,  new() { FormControlType.Text } }
    };

    /// <summary>
    /// 取得允許的控制元件清單。
    /// </summary>
    /// <param name="dataType">SQL 資料型別字串（來源為 schema）</param>
    public static List<FormControlType> GetControlTypeWhitelist(string dataType)
    {
        var sqlType = ParseSqlDataType(dataType);
        return ControlTypeWhitelistMap.TryGetValue(sqlType, out var list)
            ? list
            : ControlTypeWhitelistMap[SqlDataType.Unknown];
    }

    /// <summary>
    /// 取得預設控制元件型別。
    /// </summary>
    public static FormControlType GetDefaultControlType(string dataType)
    {
        var whitelist = GetControlTypeWhitelist(dataType);
        return whitelist.FirstOrDefault();
    }

    /// <summary>
    /// 將 SQL 資料型別字串轉換為 Enum（安全解析）
    /// </summary>
    public static SqlDataType ParseSqlDataType(string dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "int" => SqlDataType.Int,
            "decimal" => SqlDataType.Decimal,
            "bit" => SqlDataType.Bit,
            "nvarchar" => SqlDataType.NVarChar,
            "varchar" => SqlDataType.VarChar,
            "datetime" => SqlDataType.DateTime,
            "text" => SqlDataType.Text,
            _ => SqlDataType.Unknown
        };
    }
}
