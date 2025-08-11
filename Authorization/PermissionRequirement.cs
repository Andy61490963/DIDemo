using Microsoft.AspNetCore.Authorization;

namespace DynamicForm.Authorization
{
    /// <summary>
    /// 權限需求，封裝功能代碼。
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// 功能代碼，例如：FormDesigner.Edit。
        /// </summary>
        public string PermissionCode { get; }

        public PermissionRequirement(string permissionCode)
        {
            PermissionCode = permissionCode;
        }
    }
}
