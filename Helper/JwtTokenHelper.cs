using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DynamicForm.Helper
{
    /// <summary>
    /// 產生 JWT Token 的協助類別。
    /// </summary>
    public class JwtTokenHelper : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// 建構函式注入設定值。
        /// </summary>
        /// <param name="jwtOptions">JWT 設定值。</param>
        public JwtTokenHelper(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        /// <inheritdoc />
        public (string Token, DateTime Expiration) GenerateToken(UserAccount user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Account),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expiration);
        }
    }
}
