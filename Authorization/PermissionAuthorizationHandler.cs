using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DynamicForm.Areas.Permission.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace DynamicForm.Authorization
{
    /// <summary>
    /// 透過資料庫或快取驗證使用者是否具有指定權限。
    /// </summary>
    public class PermissionRequirementScopedToController : IAuthorizationRequirement
    {
        public int ActionCode { get; }
        public PermissionRequirementScopedToController(int actionCode) => ActionCode = actionCode;
    }

    public class PermissionAuthorizationHandler
        : AuthorizationHandler<PermissionRequirementScopedToController>
    {
        private readonly IPermissionService _permissionService;
        private readonly IHttpContextAccessor _http;

        public PermissionAuthorizationHandler(IPermissionService ps, IHttpContextAccessor http)
        {
            _permissionService = ps;
            _http = http;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirementScopedToController requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true) return;

            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return;

            var route = _http.HttpContext?.GetEndpoint()?.Metadata
                ?.GetMetadata<Microsoft.AspNetCore.Routing.EndpointNameMetadata>();

            // 取得 Area / Controller（用 RouteValues 最穩）
            var routeValues = _http.HttpContext?.GetRouteData()?.Values;
            var area = (routeValues?["area"]?.ToString() ?? "").Trim();
            var controller = (routeValues?["controller"]?.ToString() ?? "").Trim();

            // 呼叫 Service：只檢查這個 (Area, Controller, Action)
            var ok = await _permissionService.UserHasControllerPermissionAsync(
                userId, area, controller, requirement.ActionCode);

            if (ok) context.Succeed(requirement);
        }
    }

}
