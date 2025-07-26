using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum TableStatusType
{
    Draft = 0,   // 編輯中
    Save = 1,    // 以儲存
}