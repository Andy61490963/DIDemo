using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum ActionType
{
    [Display(Name = "新增", Description = "建立新資料")]
    Add = 0,
    
    [Display(Name = "編輯", Description = "編輯舊資料")]
    Edit = 1
}
