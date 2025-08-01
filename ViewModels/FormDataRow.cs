using DynamicForm.Models;

namespace DynamicForm.ViewModels;

public class FormDataRow
{
    public object Id { get; set; }
    public List<FormDataCell> Cells { get; set; } = new();

    public object? GetValue(string columnName)
    {
        foreach (var cell in Cells)
        {
            if (string.Equals(cell.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                return cell.Value;
        }
        return null;
    }

    public object? this[string columnName] => GetValue(columnName);
}