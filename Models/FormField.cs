using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models;

public class FormField
{
    public int FieldId { get; set; }

    [Required]
    public int FormId { get; set; }

    [Required]
    [Display(Name = "欄位標籤")]
    public string FieldLabel { get; set; }

    [Required]
    [Display(Name = "欄位 Key 名稱")]
    public string FieldKey { get; set; } // 對應表單資料欄位名，不能重複

    [Required]
    [Display(Name = "欄位類型")]
    public string FieldType { get; set; } // text, textarea, select, checkbox, date, ...

    [Display(Name = "預設值")]
    public string? DefaultValue { get; set; }

    [Display(Name = "是否必填")]
    public bool IsRequired { get; set; }

    [Display(Name = "選項 (JSON)")]
    public string? FieldOptions { get; set; } // JSON 格式（select, checkbox 專用）

    [Display(Name = "欄位排序")]
    public int? FieldOrder { get; set; }

    // 可選擴充欄位
    [Display(Name = "Placeholder")]
    public string? Placeholder { get; set; }

    [Display(Name = "CSS Class")]
    public string? CssClass { get; set; }

    [Display(Name = "驗證規則 (JSON)")]
    public string? ValidationRules { get; set; } // ex: {"minLength":5, "regex":"^abc"}
}
