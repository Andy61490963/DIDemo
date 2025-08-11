using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace DynamicForm.Service.Service
{
    /// <summary>
    /// 透過 Dapper 操作群組與權限資料。
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly SqlConnection _connection;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PermissionService(SqlConnection connection, IMemoryCache cache)
        {
            _connection = connection;
            _cache = cache;
        }

        /// <inheritdoc />
        public async Task<Guid> CreateGroupAsync(string name)
        {
            var id = Guid.NewGuid();
            const string sql = @"INSERT INTO SYS_GROUP (ID, NAME) VALUES (@Id, @Name)";
            await _connection.ExecuteAsync(sql, new { Id = id, Name = name });
            return id;
        }

        /// <inheritdoc />
        public async Task<Guid> CreatePermissionAsync(string code)
        {
            var id = Guid.NewGuid();
            const string sql = @"INSERT INTO SYS_PERMISSION (ID, CODE) VALUES (@Id, @Code)";
            await _connection.ExecuteAsync(sql, new { Id = id, Code = code });
            return id;
        }

        /// <inheritdoc />
        public Task AssignUserToGroupAsync(Guid userId, Guid groupId)
        {
            const string sql = @"INSERT INTO SYS_USER_GROUP (USER_ID, GROUP_ID) VALUES (@UserId, @GroupId)";
            _cache.Remove(GetCacheKey(userId));
            return _connection.ExecuteAsync(sql, new { UserId = userId, GroupId = groupId });
        }

        /// <inheritdoc />
        public Task AssignPermissionToGroupAsync(Guid groupId, Guid permissionId)
        {
            const string sql = @"INSERT INTO SYS_GROUP_PERMISSION (GROUP_ID, PERMISSION_ID) VALUES (@GroupId, @PermissionId)";
            return _connection.ExecuteAsync(sql, new { GroupId = groupId, PermissionId = permissionId });
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
                    JOIN SYS_GROUP_PERMISSION GP ON GP.PERMISSION_ID = P.ID
                    JOIN SYS_USER_GROUP UG ON UG.GROUP_ID = GP.GROUP_ID
                    WHERE UG.USER_ID = @UserId";
                var rows = await _connection.QueryAsync<string>(sql, new { UserId = userId });
                return new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
            });

            return permissions.Contains(permissionCode);
        }

        private static string GetCacheKey(Guid userId) => $"user_permissions:{userId}";
    }
}
