namespace DynamicForm.Helper;

public static class ConvertToColumnTypeHelper
{
    /// <summary>
    /// 型別轉換，集中管理。可以支援 int、datetime、decimal、bool、string、null…
    /// </summary>
    public static object? Convert(string? sqlType, object? value)
    {
        if (value is null) return null;
        var str = value.ToString();

        if (string.IsNullOrWhiteSpace(sqlType)) return value;

        switch (sqlType.ToLower())
        {
            case "int":
            case "bigint":
                return long.TryParse(str, out var l) ? l : null;

            case "decimal":
            case "numeric":
                return decimal.TryParse(str, out var d) ? d : null;

            case "bit":
                return str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase);

            case "datetime":
            case "smalldatetime":
            case "date":
                return DateTime.TryParse(str, out var dt) ? dt : null;

            case "nvarchar":
            case "varchar":
            case "nchar":
            case "char":
            case "text":
                return str;

            default:
                return str;
        }
    }
}