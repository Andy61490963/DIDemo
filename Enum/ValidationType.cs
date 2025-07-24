using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum ValidationType
{
    [Display(Name = "必填")]
    Required = 0,

    [Display(Name = "最大值")]
    Max = 1,

    [Display(Name = "最小值")]
    Min = 2,

    [Display(Name = "正則表達式")]
    Regex = 3,

    [Display(Name = "Email 格式")]
    Email = 4,

    [Display(Name = "數值格式")]
    Number = 5
}
