using System;

namespace DynamicForm.Models
{
    /// <summary>
    /// 使用者帳號資訊。
    /// </summary>
    public class UserAccount
    {
        /// <summary>
        /// 使用者唯一識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 登入帳號。
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 密碼雜湊值。
        /// 實務上請儲存雜湊後的密碼，而非明碼。
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 使用者角色。
        /// </summary>
        public string Role { get; set; } = string.Empty;
    }
}
