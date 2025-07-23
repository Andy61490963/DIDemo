using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models;

public class FormResult
{
    public int ResultId { get; set; }

    public int FormId { get; set; }

    [Display(Name = "填寫者")]
    public string SubmitUser { get; set; }

    public DateTime SubmitTime { get; set; } = DateTime.Now;

    [Display(Name = "填寫內容 JSON")]
    public string ResultJson { get; set; }
}
