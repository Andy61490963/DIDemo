using ClassLibrary;
using Dapper;
using DynamicForm.Areas.Permission.Interfaces;
using DynamicForm.Areas.Permission.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace DynamicForm.Areas.Permission.Services
{
    /// <summary>
    /// 透過 Dapper 操作群組與權限資料。
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly SqlConnection _con;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PermissionService(SqlConnection con, IMemoryCache cache)
        {
            _con = con;
            _cache = cache;
        }

        // 群組 CRUD
        public async Task<Guid> CreateGroupAsync(string name)
        {
            var id = Guid.NewGuid();
            const string sql =
                @"INSERT INTO SYS_GROUP (ID, NAME, IS_ACTIVE)
                  VALUES (@Id, @Name, 1)";
            await _con.ExecuteAsync(sql, new { Id = id, Name = name });
            return id;
        }

        public Task<Group?> GetGroupAsync(Guid id)
        {
            const string sql = @"SELECT ID, NAME FROM SYS_GROUP WHERE ID = @Id AND IS_ACTIVE = 1";
            return _con.QuerySingleOrDefaultAsync<Group>(sql, new { Id = id });
        }

        public Task UpdateGroupAsync(Group group)
        {
            const string sql = @"UPDATE SYS_GROUP SET NAME = @Name WHERE ID = @Id";
            return _con.ExecuteAsync(sql, new { Id = group.Id, Name = group.Name });
        }

        public Task DeleteGroupAsync(Guid id)
        {
            const string sql = @"UPDATE SYS_GROUP SET IS_ACTIVE = 0 WHERE ID = @Id";
            return _con.ExecuteAsync(sql, new { Id = id });
        }

        // 權限 CRUD
        public async Task<Guid> CreatePermissionAsync(ActionType code)
        {
            var id = Guid.NewGuid();
            const string sql =
                @"INSERT INTO SYS_PERMISSION (ID, CODE, IS_ACTIVE)
                  VALUES (@Id, @Code, 1)";
            await _con.ExecuteAsync(sql, new { Id = id, Code = code });
            return id;
        }

        public Task<PermissionModel?> GetPermissionAsync(Guid id)
        {
            const string sql = @"SELECT ID, CODE FROM SYS_PERMISSION WHERE ID = @Id AND IS_ACTIVE = 1";
            return _con.QuerySingleOrDefaultAsync<PermissionModel>(sql, new { Id = id });
        }

        public Task UpdatePermissionAsync(PermissionModel permission)
        {
            const string sql = @"UPDATE SYS_PERMISSION SET CODE = @Code WHERE ID = @Id";
            return _con.ExecuteAsync(sql, new { Id = permission.Id, Code = permission.Code });
        }

        public Task DeletePermissionAsync(Guid id)
        {
            const string sql = @"UPDATE SYS_PERMISSION SET IS_ACTIVE = 0 WHERE ID = @Id";
            return _con.ExecuteAsync(sql, new { Id = id });
        }

        // 功能 CRUD
        public async Task<Guid> CreateFunctionAsync(Function function)
        {
            var id = Guid.NewGuid();
            const string sql =
                @"INSERT INTO SYS_FUNCTION (ID, NAME, AREA, CONTROLLER, IS_DELETE)
                  VALUES (@Id, @Name, @Area, @Controller, 0)";
            await _con.ExecuteAsync(sql, new
            {
                Id = id,
                Name = function.Name,
                Area = function.Area,
                Controller = function.Controller
            });
            return id;
        }

        public Task<Function?> GetFunctionAsync(Guid id)
        {
            const string sql =
                @"SELECT ID, NAME, AREA, CONTROLLER, IS_DELETE
                  FROM SYS_FUNCTION
                  WHERE ID = @Id AND IS_DELETE = 0";
            return _con.QuerySingleOrDefaultAsync<Function>(sql, new { Id = id });
        }

        public Task UpdateFunctionAsync(Function function)
        {
            const string sql =
                @"UPDATE SYS_FUNCTION
                  SET NAME = @Name, AREA = @Area, CONTROLLER = @Controller
                  WHERE ID = @Id AND IS_DELETE = 0";
            return _con.ExecuteAsync(sql, new
            {
                Id = function.Id,
                Name = function.Name,
                Area = function.Area,
                Controller = function.Controller
            });
        }

        public Task DeleteFunctionAsync(Guid id)
        {
            const string sql = @"UPDATE SYS_FUNCTION SET IS_DELETE = 1 WHERE ID = @Id";
            return _con.ExecuteAsync(sql, new { Id = id });
        }

        // 選單 CRUD
        public async Task<Guid> CreateMenuAsync(Menu menu)
        {
            var id = Guid.NewGuid();
            const string sql =
                @"INSERT INTO SYS_MENU (ID, PARENT_ID, SYS_FUNCTION_ID, NAME, SORT, IS_SHARE, IS_DELETE)
                  VALUES (@Id, @ParentId, @FuncId, @Name, @Sort, @IsShare, 0)";
            await _con.ExecuteAsync(sql, new
            {
                Id = id,
                ParentId = menu.ParentId,
                FuncId = menu.SysFunctionId,
                Name = menu.Name,
                Sort = menu.Sort,
                IsShare = menu.IsShare
            });
            return id;
        }

        public Task<Menu?> GetMenuAsync(Guid id)
        {
            const string sql =
                @"SELECT ID, PARENT_ID AS ParentId, SYS_FUNCTION_ID AS SysFunctionId,
                         NAME, SORT, IS_SHARE, IS_DELETE
                  FROM SYS_MENU
                  WHERE ID = @Id AND IS_DELETE = 0";
            return _con.QuerySingleOrDefaultAsync<Menu>(sql, new { Id = id });
        }

        public Task UpdateMenuAsync(Menu menu)
        {
            const string sql =
                @"UPDATE SYS_MENU
                  SET PARENT_ID = @ParentId,
                      SYS_FUNCTION_ID = @FuncId,
                      NAME = @Name,
                      SORT = @Sort,
                      IS_SHARE = @IsShare
                  WHERE ID = @Id AND IS_DELETE = 0";
            return _con.ExecuteAsync(sql, new
            {
                Id = menu.Id,
                ParentId = menu.ParentId,
                FuncId = menu.SysFunctionId,
                Name = menu.Name,
                Sort = menu.Sort,
                IsShare = menu.IsShare
            });
        }

        public Task DeleteMenuAsync(Guid id)
        {
            const string sql = @"UPDATE SYS_MENU SET IS_DELETE = 1 WHERE ID = @Id";
            return _con.ExecuteAsync(sql, new { Id = id });
        }

        // 使用者與群組關聯
        public Task AssignUserToGroupAsync(Guid userId, Guid groupId)
        {
            var id = Guid.NewGuid();
            const string sql =
                @"INSERT INTO SYS_USER_GROUP (ID, SYS_USER_ID, SYS_GROUP_ID)
                  VALUES (@Id, @UserId, @GroupId)";
            _cache.Remove(GetCacheKey(userId));
            return _con.ExecuteAsync(sql, new { Id = id, UserId = userId, GroupId = groupId });
        }

        public Task RemoveUserFromGroupAsync(Guid userId, Guid groupId)
        {
            const string sql =
                @"DELETE FROM SYS_USER_GROUP
                  WHERE SYS_USER_ID = @UserId AND SYS_GROUP_ID = @GroupId";
            _cache.Remove(GetCacheKey(userId));
            return _con.ExecuteAsync(sql, new { UserId = userId, GroupId = groupId });
        }

        // 群組與功能權限關聯
        public Task AssignGroupFunctionPermissionAsync(Guid groupId, Guid functionId, Guid permissionId)
        {
            var id = Guid.NewGuid();
            const string sql =
                @"INSERT INTO SYS_GROUP_FUNCTION_PERMISSION (ID, SYS_GROUP_ID, SYS_FUNCTION_ID, SYS_PERMISSION_ID)
                  VALUES (@Id, @GroupId, @FunctionId, @PermissionId)";
            return _con.ExecuteAsync(sql, new
            {
                Id = id,
                GroupId = groupId,
                FunctionId = functionId,
                PermissionId = permissionId
            });
        }

        public Task RemoveGroupFunctionPermissionAsync(Guid groupId, Guid functionId, Guid permissionId)
        {
            const string sql =
                @"DELETE FROM SYS_GROUP_FUNCTION_PERMISSION
                  WHERE SYS_GROUP_ID = @GroupId AND SYS_FUNCTION_ID = @FunctionId AND SYS_PERMISSION_ID = @PermissionId";
            return _con.ExecuteAsync(sql, new
            {
                GroupId = groupId,
                FunctionId = functionId,
                PermissionId = permissionId
            });
        }

        // 權限檢查
        public async Task<bool> UserHasControllerPermissionAsync(Guid userId, string area, string controller, int actionCode)
        {
            var cacheKey = $"perm:{userId}:{area}:{controller}:{actionCode}";
            if (_cache.TryGetValue(cacheKey, out bool ok)) return ok;

            const string sql =
                @"SELECT CASE WHEN EXISTS (
                      SELECT 1
                      FROM SYS_USER_GROUP ug
                      JOIN SYS_GROUP_FUNCTION_PERMISSION gfp
                        ON gfp.SYS_GROUP_ID = ug.SYS_GROUP_ID
                      JOIN SYS_FUNCTION f
                        ON f.ID = gfp.SYS_FUNCTION_ID
                       AND f.IS_DELETE = 0
                      JOIN SYS_PERMISSION p
                        ON p.ID = gfp.SYS_PERMISSION_ID
                      WHERE ug.SYS_USER_ID = @UserId
                        AND f.AREA       = @Area
                        AND f.CONTROLLER = @Controller
                        AND p.CODE       = @ActionCode
                    ) THEN 1 ELSE 0 END AS HasPermission;";

            var has = await _con.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                Area = area ?? string.Empty,
                Controller = controller,
                ActionCode = actionCode
            }) > 0;

            _cache.Set(cacheKey, has, TimeSpan.FromSeconds(60));
            return has;
        }

        private static string GetCacheKey(Guid userId) => $"user_permissions:{userId}";
    }
}

