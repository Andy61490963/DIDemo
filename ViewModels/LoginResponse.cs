using System;

namespace DynamicForm.ViewModels
{
    /// <summary>
    /// 登入結果回傳給前端的內容。
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// JWT Token 字串。
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Token 到期時間。
        /// </summary>
        public DateTime Expiration { get; set; }
    }
}
