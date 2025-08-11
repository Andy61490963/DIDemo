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

        /// <inheritdoc />
        public async Task<bool> UserHasPermissionAsync(Guid userId, string permissionCode)
        {
            var permissions = await _cache.GetOrCreateAsync(GetCacheKey(userId), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                const string sql = @"
                    SELECT P.CODE
                    FROM SYS_PERMISSION P
                    JOIN SYS_GROUP_PERMISSION GP ON GP.SYS_PERMISSION_ID = P.ID
                    JOIN SYS_USER_GROUP UG ON UG.SYS_GROUP_ID = GP.SYS_GROUP_ID
                    WHERE UG.SYS_USER_ID = @UserId";
                var rows = await _con.QueryAsync<string>(sql, new { UserId = userId });
                return new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
            });

            return permissions.Contains(permissionCode);
        }

        private static string GetCacheKey(Guid userId) => $"user_permissions:{userId}";
    }
}
