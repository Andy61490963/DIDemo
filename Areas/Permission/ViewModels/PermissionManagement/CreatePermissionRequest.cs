using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Areas.Permission.ViewModels.PermissionManagement
{
    /// <summary>
    /// 建立權限的請求資料。
    /// </summary>
    public class CreatePermissionRequest
    {
        /// <summary>
        /// 權限代碼，例如：FormDesigner.Edit。
        /// </summary>
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
