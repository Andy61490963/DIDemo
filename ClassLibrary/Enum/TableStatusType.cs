using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum TableStatusType
{
    [Display(Name = "編輯中")]
    Draft = 0,   
    
    [Display(Name = "啟用")]
    Active = 1,  
    
    [Display(Name = "停用")]
    Disabled = 2 
}