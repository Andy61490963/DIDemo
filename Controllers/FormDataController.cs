using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers;

public class FormDataController : Controller
{
    private readonly IFormService _service;

    public FormDataController(IFormService service)
    {
        _service = service;
    }

    public IActionResult Index(Guid id)
    {
        var vm = _service.GetFormList(id);
        return View(vm);
    }
}
