using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

/// <summary>
/// 查詢條件元件類型，用於決定搜尋介面所使用的輸入元件。
/// </summary>
public enum QueryConditionType
{
    /// <summary>
    /// 以單行文字輸入作為條件。
    /// </summary>
    [Display(Name = "文字")]
    Text = 0,

    /// <summary>
    /// 數值輸入條件。
    /// </summary>
    [Display(Name = "數字")]
    Number = 1,

    /// <summary>
    /// 日期輸入條件。
    /// </summary>
    [Display(Name = "日期")]
    Date = 2,

    /// <summary>
    /// 下拉選單條件。
    /// </summary>
    [Display(Name = "下拉選單")]
    Dropdown = 3
}
