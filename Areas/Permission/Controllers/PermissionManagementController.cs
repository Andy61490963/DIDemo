using DynamicForm.Areas.Permission.Models;
using DynamicForm.Areas.Permission.Interfaces;
using DynamicForm.Areas.Permission.ViewModels.PermissionManagement;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Areas.Permission.Controllers
{
    /// <summary>
    /// 提供群組與權限管理的 API。
    /// </summary>
    [Area("Permission")]
    [ApiController]
    [Route("[area]/[controller]")]
    public class PermissionManagementController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionManagementController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// 建立群組。
        /// </summary>
        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            var id = await _permissionService.CreateGroupAsync(request.Name);
            return Ok(new Group { Id = id, Name = request.Name });
        }

        /// <summary>
        /// 建立權限。
        /// </summary>
        [HttpPost("permissions")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            var id = await _permissionService.CreatePermissionAsync(request.Code);
            return Ok(new Permission { Id = id, Code = request.Code });
        }

        /// <summary>
        /// 將使用者加入群組。
        /// </summary>
        [HttpPost("groups/{groupId}/users")]
        public async Task<IActionResult> AssignUserToGroup(Guid groupId, [FromBody] AssignUserGroupRequest request)
        {
            await _permissionService.AssignUserToGroupAsync(request.UserId, groupId);
            return Ok();
        }

        /// <summary>
        /// 將權限指派給群組。
        /// </summary>
        [HttpPost("groups/{groupId}/permissions")]
        public async Task<IActionResult> AssignPermissionToGroup(Guid groupId, [FromBody] AssignGroupPermissionRequest request)
        {
            await _permissionService.AssignPermissionToGroupAsync(groupId, request.PermissionId);
            return Ok();
        }
    }
}
