using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum TableSchemaQueryType
{
    OnlyTable = 0,   // 只查實體表
    OnlyView = 1,    // 只查 View（V_ 開頭）
    All = 2        
}