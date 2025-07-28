namespace DynamicForm.Models
{
    /// <summary>
    /// 資料列表用 ViewModel
    /// </summary>
    public class FormListDataViewModel
    {
        public Guid FormId { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
    }
}