using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models;

public class FormMaster
{
    public int FormId { get; set; }

    [Required]
    [Display(Name = "表單名稱")]
    public string FormName { get; set; }

    [Display(Name = "表單描述")]
    public string Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}
