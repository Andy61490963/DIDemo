using System;
using Microsoft.AspNetCore.Authorization;
using DynamicForm.Helper;

namespace DynamicForm.Authorization
{
    /// <summary>
    /// 指定存取此端點所需的權限代碼。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// 用於區分權限政策的前綴字串。
        /// </summary>
        public const string PolicyPrefix = "PERM_";

        /// <summary>
        /// 建立 RequirePermissionAttribute。
        /// </summary>
        /// <param name="permissionCode">功能代碼（例如：FormDesigner.Edit）。</param>
        public RequirePermissionAttribute(string permissionCode)
        {
            // 將 Policy 設為帶有前綴的權限名稱，讓 PolicyProvider 能解析。
            Policy = PolicyPrefix + permissionCode;
        }

        /// <summary>
        /// 以 <see cref="ActionAuthorize"/> 指定權限。
        /// </summary>
        /// <param name="action">預先定義的權限列舉。</param>
        public RequirePermissionAttribute(ActionAuthorize action)
            : this(action.ToString())
        {
        }

        /// <summary>
        /// 取得實際用於 Policy 的名稱。
        /// </summary>
        public static string GetPolicy(string permissionCode) => PolicyPrefix + permissionCode;
    }
}
