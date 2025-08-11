using System;

namespace DynamicForm.Models
{
    /// <summary>
    /// 功能權限。
    /// </summary>
    public class Permission
    {
        /// <summary>
        /// 權限唯一識別碼。
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 權限代碼，例如：FormDesigner.Edit。
        /// </summary>
        public string Code { get; set; } = string.Empty;
    }
}
