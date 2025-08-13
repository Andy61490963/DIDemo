using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using DynamicForm.Areas.Permission.Interfaces;
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

        /// <inheritdoc />
        public async Task<Guid> CreateGroupAsync(string name)
        {
            var id = Guid.NewGuid();
            const string sql = @"INSERT INTO SYS_GROUP (ID, NAME, IS_ACTIVE) VALUES (@Id, @Name, 1)";
            await _con.ExecuteAsync(sql, new { Id = id, Name = name });
            return id;
        }

        /// <inheritdoc />
        public async Task<Guid> CreatePermissionAsync(string code)
        {
            var id = Guid.NewGuid();
            const string sql = @"INSERT INTO SYS_PERMISSION (ID, CODE, IS_ACTIVE) VALUES (@Id, @Code, 1)";
            await _con.ExecuteAsync(sql, new { Id = id, Code = code });
            return id;
        }

        /// <inheritdoc />
        public Task AssignUserToGroupAsync(Guid userId, Guid groupId)
        {
            var id = Guid.NewGuid();
            const string sql = @"INSERT INTO SYS_USER_GROUP (ID, SYS_USER_ID, SYS_GROUP_ID) VALUES (@Id, @UserId, @GroupId)";
            _cache.Remove(GetCacheKey(userId));
            return _con.ExecuteAsync(sql, new { Id = id, UserId = userId, GroupId = groupId });
        }

        /// <inheritdoc />
        public Task AssignPermissionToGroupAsync(Guid groupId, Guid permissionId)
        {
            var id = Guid.NewGuid();
            const string sql = @"INSERT INTO SYS_GROUP_PERMISSION (ID, SYS_GROUP_ID, SYS_PERMISSION_ID) VALUES (@Id, @GroupId, @PermissionId)";
            return _con.ExecuteAsync(sql, new { Id = id, GroupId = groupId, PermissionId = permissionId });
        }
        
        public async Task<bool> UserHasControllerPermissionAsync(
            Guid userId, string area, string controller, int actionCode)
        {
            // 每個 (user, area, ctrl, action) 快取 60 秒，可視情況拉長/用 Redis
            var cacheKey = $"perm:{userId}:{area}:{controller}:{actionCode}";
            if (_cache.TryGetValue(cacheKey, out bool ok)) return ok;

            const string sql = @"/**/
SELECT CASE WHEN EXISTS (
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
) THEN 1 ELSE 0 END AS HasPermission;
";

            var has = await _con.ExecuteScalarAsync<int>(sql, new {
                UserId = userId,
                Area = area ?? "",
                Controller = controller,
                ActionCode = actionCode
            }) > 0;

            _cache.Set(cacheKey, has, TimeSpan.FromSeconds(60));
            return has;
        }
        
        private static string GetCacheKey(Guid userId) => $"user_permissions:{userId}";
    }
}
