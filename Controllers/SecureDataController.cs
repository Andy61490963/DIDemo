using DynamicForm.Authorization;
using DynamicForm.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers
{
    /// <summary>
    /// 需要 JWT 授權才能存取的範例端點。
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/secure")]
    public class SecureDataController : ControllerBase
    {
        /// <summary>
        /// 取得受保護的資料。
        /// </summary>
        /// <returns>簡單的訊息。</returns>
        [RequirePermission(ActionAuthorizeHelper.Import)]
        [HttpGet("data")]
        public IActionResult GetSecureData()
        {
            return Ok(new { Message = "This is protected data." });
        }
    }
}
