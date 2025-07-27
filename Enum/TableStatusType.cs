using System.ComponentModel.DataAnnotations;

namespace ClassLibrary;

public enum TableStatusType
{
    Draft = 0,    // 編輯中
    Active = 1,   // 啟用
    Disabled = 2  // 停用
}