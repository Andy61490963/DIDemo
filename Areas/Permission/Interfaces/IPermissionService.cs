using System;
using System.Threading.Tasks;
using DynamicForm.Areas.Permission.Models;

namespace DynamicForm.Areas.Permission.Interfaces
{
    /// <summary>
    /// 提供群組與權限相關操作。
    /// </summary>
    public interface IPermissionService
    {
        // 使用者
        Task<Guid> CreateUserAsync(User user);
        Task<User?> GetUserAsync(Guid id);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(Guid id);

        // 群組
        Task<Guid> CreateGroupAsync(string name);
        Task<Group?> GetGroupAsync(Guid id);
        Task UpdateGroupAsync(Group group);
        Task DeleteGroupAsync(Guid id);

        // 權限
        Task<Guid> CreatePermissionAsync(string code);
        Task<Permission?> GetPermissionAsync(Guid id);
        Task UpdatePermissionAsync(Permission permission);
        Task DeletePermissionAsync(Guid id);

        // 功能
        Task<Guid> CreateFunctionAsync(Function function);
        Task<Function?> GetFunctionAsync(Guid id);
        Task UpdateFunctionAsync(Function function);
        Task DeleteFunctionAsync(Guid id);

        // 選單
        Task<Guid> CreateMenuAsync(Menu menu);
        Task<Menu?> GetMenuAsync(Guid id);
        Task UpdateMenuAsync(Menu menu);
        Task DeleteMenuAsync(Guid id);

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

