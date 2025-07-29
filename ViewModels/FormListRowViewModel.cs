namespace DynamicForm.Models
{
    /// <summary>
    /// 單筆資料列 ViewModel
    /// </summary>
    public class FormListRowViewModel
    {
        public Guid Id { get; set; }
        public List<object?> Values { get; set; } = new();
    }
}
