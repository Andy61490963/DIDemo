namespace DynamicForm.Helper;

/// <summary>
/// 操作類型列舉，限制可使用的權限代碼。
/// </summary>
public enum ActionAuthorize
{
    /// <summary>
    /// 檢視
    /// </summary>
    View,

    /// <summary>
    /// 資料異動
    /// </summary>
    Update,

    /// <summary>
    /// 匯入
    /// </summary>
    Import,

    /// <summary>
    /// 匯出
    /// </summary>
    Export
}

