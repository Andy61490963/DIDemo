using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum FormControlType
{
    [Display(Name = "文字")]
    Text,
    
    [Display(Name = "數字")]
    Number,
    
    [Display(Name = "日期")]
    Date,
    
    [Display(Name = "確認按鈕")]
    Checkbox,
    
    [Display(Name = "文字輸入框")]
    Textarea,
    
    [Display(Name = "下拉選單")]
    Dropdown
}
