using System.Threading.Tasks;
using DynamicForm.Service.Interface;
using DynamicForm.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers
{
    /// <summary>
    /// 處理登入與取得 JWT 的 API。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IAuthService _authService;

        /// <summary>
        /// 建構函式注入驗證服務。
        /// </summary>
        /// <param name="authService">驗證服務。</param>
        public LoginController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 登入並取得 JWT Token。
        /// </summary>
        /// <param name="request">帳號與密碼。</param>
        /// <returns>JWT Token。</returns>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.AuthenticateAsync(request.Account, request.Password);
            if (result == null)
            {
                return Unauthorized();
            }

            return Ok(result);
        }
    }
}
