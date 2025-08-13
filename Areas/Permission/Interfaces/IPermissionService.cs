using System;
using System.Threading.Tasks;

namespace DynamicForm.Areas.Permission.Interfaces
{
    /// <summary>
    /// 提供群組與權限相關操作。
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 建立新的群組。
        /// </summary>
        Task<Guid> CreateGroupAsync(string name);

        /// <summary>
        /// 建立新的權限。
        /// </summary>
        Task<Guid> CreatePermissionAsync(string code);

        /// <summary>
        /// 將使用者加入群組。
        /// </summary>
        Task AssignUserToGroupAsync(Guid userId, Guid groupId);

        /// <summary>
        /// 將權限指派給群組。
        /// </summary>
        Task AssignPermissionToGroupAsync(Guid groupId, Guid permissionId);

        /// <summary>
        /// 檢查使用者是否擁有指定權限。
        /// </summary>
        Task<bool> UserHasControllerPermissionAsync(
            Guid userId, string area, string controller, int actionCode);
    }
}
