using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Authorization;

namespace DynamicForm.Authorization
{
    /// <summary>
    /// 透過資料庫或快取驗證使用者是否具有指定權限。
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationHandler(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <inheritdoc />
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return; // 未登入則直接拒絕
            }

            var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return; // 取不到使用者 ID
            }

            var hasPermission = await _permissionService.UserHasPermissionAsync(userId, requirement.PermissionCode);
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}
