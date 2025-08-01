using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormListController : ControllerBase
{
    private readonly IFormListService _service;

    public FormListController(IFormListService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetFormMasters(string? q)
    {
        var list = _service.GetFormMasters();
        if (!string.IsNullOrWhiteSpace(q))
        {
            list = list
                .Where(x => x.FORM_NAME != null && x.FORM_NAME.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        return Ok(list);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _service.DeleteFormMaster(id);
        return NoContent();
    }
}
