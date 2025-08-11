using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum TableSchemaQueryType
{
    [Display(Name = "主表")]
    OnlyTable = 0,   
    
    [Display(Name = "檢視表")]
    OnlyView = 1,   
    
    [Display(Name = "主表與檢視表")]
    All = 2        
}