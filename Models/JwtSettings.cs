namespace DynamicForm.Models
{
    /// <summary>
    /// JWT 設定值，對應 appsettings.json 中的 JwtSettings 區段。
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Token 發行者。
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Token 受眾。
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// 用於簽章的秘密金鑰。
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;
    }
}
