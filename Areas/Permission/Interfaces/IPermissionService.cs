using ClassLibrary;
using DynamicForm.Areas.Permission.Models;

namespace DynamicForm.Areas.Permission.Interfaces
{
    /// <summary>
    /// 提供群組與權限相關操作。
    /// </summary>
    public interface IPermissionService
    {
        // 群組
        Task<Guid> CreateGroupAsync(string name);
        Task<Group?> GetGroupAsync(Guid id);
        Task UpdateGroupAsync(Group group);
        Task DeleteGroupAsync(Guid id);
        Task<bool> GroupNameExistsAsync(string name, Guid? excludeId = null);

        // 權限
        Task<Guid> CreatePermissionAsync(ActionType code);
        Task<PermissionModel?> GetPermissionAsync(Guid id);
        Task UpdatePermissionAsync(PermissionModel permission);
        Task DeletePermissionAsync(Guid id);

        // 功能
        Task<Guid> CreateFunctionAsync(Function function);
        Task<Function?> GetFunctionAsync(Guid id);
        Task UpdateFunctionAsync(Function function);
        Task DeleteFunctionAsync(Guid id);
        Task<bool> FunctionNameExistsAsync(string name, Guid? excludeId = null);

        // 選單
        Task<Guid> CreateMenuAsync(Menu menu);
        Task<Menu?> GetMenuAsync(Guid id);
        Task UpdateMenuAsync(Menu menu);
        Task DeleteMenuAsync(Guid id);
        Task<bool> MenuNameExistsAsync(string name, Guid? parentId, Guid? excludeId = null);

        // 使用者與群組關聯
        Task AssignUserToGroupAsync(Guid userId, Guid groupId);
        Task RemoveUserFromGroupAsync(Guid userId, Guid groupId);

        // 群組與功能權限關聯
        Task AssignGroupFunctionPermissionAsync(Guid groupId, Guid functionId, Guid permissionId);
        Task RemoveGroupFunctionPermissionAsync(Guid groupId, Guid functionId, Guid permissionId);

        // 權限檢查
        Task<bool> UserHasControllerPermissionAsync(Guid userId, string area, string controller, int actionCode);
    }
}

