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
    public class AuthenticationService : IAuthenticationService
    {
        private readonly SqlConnection _connection;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;

        /// <summary>
        /// 建構函式注入相依物件。
        /// </summary>
        /// <param name="connection">資料庫連線。</param>
        /// <param name="passwordHasher">密碼雜湊器。</param>
        /// <param name="tokenGenerator">Token 產生器。</param>
        public AuthenticationService(
            SqlConnection connection,
            IPasswordHasher passwordHasher,
            ITokenGenerator tokenGenerator)
        {
            _connection = connection;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
        }

        /// <inheritdoc />
        public async Task<LoginResponse?> AuthenticateAsync(string account, string password)
        {
            const string sql = @"SELECT ID, NAME AS Account, SWD AS PasswordHash, PASSWORD_SALT AS PasswordSalt, ROLE FROM UserAccount WHERE NAME = @Account AND IS_DELETE = 0";
            var user = await _connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new { Account = account });
            if (user == null)
            {
                return null;
            }

            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            var tokenResult = _tokenGenerator.GenerateToken(user);
            return new LoginResponse
            {
                Token = tokenResult.Token,
                Expiration = tokenResult.Expiration,
                RefreshToken = tokenResult.RefreshToken,
                RefreshTokenExpiration = tokenResult.RefreshTokenExpiration
            };
        }
    }
}
