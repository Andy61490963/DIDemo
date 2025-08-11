namespace DynamicForm.ViewModels
{
    /// <summary>
    /// 登入請求內容。
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 使用者帳號。
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 使用者密碼。
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
