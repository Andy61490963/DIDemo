using System.Threading.Tasks;
using DynamicForm.ViewModels;

namespace DynamicForm.Service.Interface
{
    /// <summary>
    /// 處理使用者驗證相關的服務介面。
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 驗證使用者帳密並產生 JWT。
        /// </summary>
        /// <param name="account">登入帳號。</param>
        /// <param name="password">登入密碼。</param>
        /// <returns>成功則回傳含 Token 的結果，失敗則為 null。</returns>
        Task<LoginResponse?> AuthenticateAsync(string account, string password);
    }
}
