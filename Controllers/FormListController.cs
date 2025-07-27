using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers;

public class FormListController : Controller
{
    private readonly IFormListService _service;

    public FormListController(IFormListService service)
    {
        _service = service;
    }

    public IActionResult Index(string? q)
    {
        var list = _service.GetFormMasters();
        if (!string.IsNullOrWhiteSpace(q))
        {
            list = list.Where(x => x.FORM_NAME != null && x.FORM_NAME.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewBag.Query = q;
        return View(list);
    }

    [HttpPost]
    public IActionResult Delete(Guid id)
    {
        _service.DeleteFormMaster(id);
        return RedirectToAction("Index");
    }
}
