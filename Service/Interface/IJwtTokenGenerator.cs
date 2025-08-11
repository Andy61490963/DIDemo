using DynamicForm.Models;
using System;

namespace DynamicForm.Service.Interface
{
    /// <summary>
    /// 產生 JWT Token 的介面。
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// 依據使用者資訊產生 JWT Token。
        /// </summary>
        /// <param name="user">使用者資訊。</param>
        /// <returns>Token 字串與到期時間。</returns>
        (string Token, DateTime Expiration) GenerateToken(UserAccount user);
    }
}
