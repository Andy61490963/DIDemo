using Microsoft.AspNetCore.Mvc;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Service;
using Microsoft.AspNetCore.Http;

namespace DynamicForm.Controllers;

public class FormController : Controller
{
    private readonly IFormBuilderService _formBuilderService;

    public FormController(IFormBuilderService formBuilderService)
    {
        _formBuilderService = formBuilderService;
    }

    // 顯示填寫畫面
    public IActionResult Fill(int id)
    {
        var form = _formBuilderService.GetFormById(id);
        var fields = _formBuilderService.GetFieldsByFormId(id);
        return View(new FormFillViewModel
        {
            Form = form,
            Fields = fields
        });
    }

    // 接收送出
    [HttpPost]
    public IActionResult Submit(IFormCollection form)
    {
        int formId = Convert.ToInt32(form["FormId"]);
        var fields = _formBuilderService.GetFieldsByFormId(formId);

        var result = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            result[field.FieldKey] = form[field.FieldKey];
        }

        string json = System.Text.Json.JsonSerializer.Serialize(result);

        _formBuilderService.SaveResult(new FormResult
        {
            FormId = formId,
            SubmitUser = User.Identity?.Name ?? "匿名使用者",
            ResultJson = json
        });

        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        return Content("表單已成功提交！");
    }
}