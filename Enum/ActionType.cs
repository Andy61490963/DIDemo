using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum ActionType
{
    [Display(Name = "新增")]
    Add = 0,
    
    [Display(Name = "編輯")]
    Edit = 1
}
