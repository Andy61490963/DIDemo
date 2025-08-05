using DynamicForm.Models;

namespace DynamicForm.ViewModels
{
    /// <summary>
    /// 資料列表用 ViewModel
    /// </summary>
    public class FormListDataViewModel
    {
        public Guid FormMasterId { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<FormDataRow> Rows { get; set; } = new();
    }
}
