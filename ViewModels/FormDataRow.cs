namespace DynamicForm.Models;

public class FormDataCell
{
    public string ColumnName { get; set; } = string.Empty;
    public object? Value { get; set; }
}

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

public class DropdownAnswerDto
{
    public string RowId { get; set; } = default!;
    public Guid FieldId { get; set; }
    public Guid OptionId { get; set; }
}