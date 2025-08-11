using System.Threading.Tasks;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.ViewModels;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service
{
    /// <summary>
    /// 使用者驗證服務。
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly SqlConnection _connection;
        private readonly IJwtTokenGenerator _tokenGenerator;

        /// <summary>
        /// 建構函式注入相依物件。
        /// </summary>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="tokenGenerator">JWT 產生器。</param>
        public AuthService(SqlConnection connection, IJwtTokenGenerator tokenGenerator)
        {
            _connection = connection;
            _tokenGenerator = tokenGenerator;
        }

        /// <inheritdoc />
        public async Task<LoginResponse?> AuthenticateAsync(string account, string password)
        {
            const string sql = @"SELECT Id, Account, PasswordHash, Role FROM UserAccount WHERE Account = @Account";
            var user = await _connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { Account = account });
            if (user == null)
            {
                return null;
            }

            // TODO: 密碼應使用雜湊與鹽值比對，此處為示範僅作字串比較。
            if (!string.Equals(user.PasswordHash, password))
            {
                return null;
            }

            var (token, expiration) = _tokenGenerator.GenerateToken(user);
            return new LoginResponse { Token = token, Expiration = expiration };
        }
    }
}
