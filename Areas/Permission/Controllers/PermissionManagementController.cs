using System;
using System.Threading.Tasks;
using DynamicForm.Areas.Permission.Interfaces;
using DynamicForm.Areas.Permission.Models;
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

        // 使用者 CRUD
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var id = await _permissionService.CreateUserAsync(new User
            {
                Account = request.Account,
                Name = request.Name,
                PasswordHash = request.PasswordHash,
                PasswordSalt = request.PasswordSalt
            });
            return Ok(new User { Id = id, Account = request.Account, Name = request.Name });
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _permissionService.GetUserAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            await _permissionService.UpdateUserAsync(new User
            {
                Id = id,
                Account = request.Account,
                Name = request.Name,
                PasswordHash = request.PasswordHash,
                PasswordSalt = request.PasswordSalt
            });
            return Ok();
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            await _permissionService.DeleteUserAsync(id);
            return Ok();
        }

        // 群組 CRUD
        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            var id = await _permissionService.CreateGroupAsync(request.Name);
            return Ok(new Group { Id = id, Name = request.Name });
        }

        [HttpGet("groups/{id}")]
        public async Task<IActionResult> GetGroup(Guid id)
        {
            var group = await _permissionService.GetGroupAsync(id);
            return group == null ? NotFound() : Ok(group);
        }

        [HttpPut("groups/{id}")]
        public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateGroupRequest request)
        {
            await _permissionService.UpdateGroupAsync(new Group { Id = id, Name = request.Name });
            return Ok();
        }

        [HttpDelete("groups/{id}")]
        public async Task<IActionResult> DeleteGroup(Guid id)
        {
            await _permissionService.DeleteGroupAsync(id);
            return Ok();
        }

        // 權限 CRUD
        [HttpPost("permissions")]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            var id = await _permissionService.CreatePermissionAsync(request.Code);
            return Ok(new Permission { Id = id, Code = request.Code });
        }

        [HttpGet("permissions/{id}")]
        public async Task<IActionResult> GetPermission(Guid id)
        {
            var permission = await _permissionService.GetPermissionAsync(id);
            return permission == null ? NotFound() : Ok(permission);
        }

        [HttpPut("permissions/{id}")]
        public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
        {
            await _permissionService.UpdatePermissionAsync(new Permission { Id = id, Code = request.Code });
            return Ok();
        }

        [HttpDelete("permissions/{id}")]
        public async Task<IActionResult> DeletePermission(Guid id)
        {
            await _permissionService.DeletePermissionAsync(id);
            return Ok();
        }

        // 功能 CRUD
        [HttpPost("functions")]
        public async Task<IActionResult> CreateFunction([FromBody] CreateFunctionRequest request)
        {
            var id = await _permissionService.CreateFunctionAsync(new Function
            {
                Name = request.Name,
                Area = request.Area,
                Controller = request.Controller
            });
            return Ok(new Function { Id = id, Name = request.Name, Area = request.Area, Controller = request.Controller });
        }

        [HttpGet("functions/{id}")]
        public async Task<IActionResult> GetFunction(Guid id)
        {
            var function = await _permissionService.GetFunctionAsync(id);
            return function == null ? NotFound() : Ok(function);
        }

        [HttpPut("functions/{id}")]
        public async Task<IActionResult> UpdateFunction(Guid id, [FromBody] UpdateFunctionRequest request)
        {
            await _permissionService.UpdateFunctionAsync(new Function
            {
                Id = id,
                Name = request.Name,
                Area = request.Area,
                Controller = request.Controller
            });
            return Ok();
        }

        [HttpDelete("functions/{id}")]
        public async Task<IActionResult> DeleteFunction(Guid id)
        {
            await _permissionService.DeleteFunctionAsync(id);
            return Ok();
        }

        // 選單 CRUD
        [HttpPost("menus")]
        public async Task<IActionResult> CreateMenu([FromBody] CreateMenuRequest request)
        {
            var id = await _permissionService.CreateMenuAsync(new Menu
            {
                ParentId = request.ParentId,
                SysFunctionId = request.SysFunctionId,
                Name = request.Name,
                Sort = request.Sort,
                IsShare = request.IsShare
            });
            return Ok(new Menu
            {
                Id = id,
                ParentId = request.ParentId,
                SysFunctionId = request.SysFunctionId,
                Name = request.Name,
                Sort = request.Sort,
                IsShare = request.IsShare
            });
        }

        [HttpGet("menus/{id}")]
        public async Task<IActionResult> GetMenu(Guid id)
        {
            var menu = await _permissionService.GetMenuAsync(id);
            return menu == null ? NotFound() : Ok(menu);
        }

        [HttpPut("menus/{id}")]
        public async Task<IActionResult> UpdateMenu(Guid id, [FromBody] UpdateMenuRequest request)
        {
            await _permissionService.UpdateMenuAsync(new Menu
            {
                Id = id,
                ParentId = request.ParentId,
                SysFunctionId = request.SysFunctionId,
                Name = request.Name,
                Sort = request.Sort,
                IsShare = request.IsShare
            });
            return Ok();
        }

        [HttpDelete("menus/{id}")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            await _permissionService.DeleteMenuAsync(id);
            return Ok();
        }

        // 使用者與群組
        [HttpPost("groups/{groupId}/users")]
        public async Task<IActionResult> AssignUserToGroup(Guid groupId, [FromBody] AssignUserGroupRequest request)
        {
            await _permissionService.AssignUserToGroupAsync(request.UserId, groupId);
            return Ok();
        }

        [HttpDelete("groups/{groupId}/users/{userId}")]
        public async Task<IActionResult> RemoveUserFromGroup(Guid groupId, Guid userId)
        {
            await _permissionService.RemoveUserFromGroupAsync(userId, groupId);
            return Ok();
        }

        // 群組與功能權限
        [HttpPost("groups/{groupId}/function-permissions")]
        public async Task<IActionResult> AssignGroupFunctionPermission(Guid groupId, [FromBody] AssignGroupFunctionPermissionRequest request)
        {
            await _permissionService.AssignGroupFunctionPermissionAsync(groupId, request.FunctionId, request.PermissionId);
            return Ok();
        }

        [HttpDelete("groups/{groupId}/functions/{functionId}/permissions/{permissionId}")]
        public async Task<IActionResult> RemoveGroupFunctionPermission(Guid groupId, Guid functionId, Guid permissionId)
        {
            await _permissionService.RemoveGroupFunctionPermissionAsync(groupId, functionId, permissionId);
            return Ok();
        }
    }
}

