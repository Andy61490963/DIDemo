using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers;

public class FormBuilderController : Controller
{
    private readonly IFormBuilderService _formBuilderService;

    public FormBuilderController(IFormBuilderService formBuilderService)
    {
        _formBuilderService = formBuilderService;
    }

    // 表單清單
    public IActionResult Index()
    {
        var forms = _formBuilderService.GetAllForms();
        return View(forms);
    }

    // 編輯/新增表單
    [HttpGet]
    public IActionResult Edit(int? id)
    {
        if (id.HasValue)
        {
            var form = _formBuilderService.GetFormById(id.Value);
            var fields = _formBuilderService.GetFieldsByFormId(id.Value);
            return View(new FormFillViewModel { Form = form, Fields = fields });
        }
        return View(new FormFillViewModel
        {
            Form = new FormMaster(),
            Fields = new List<FormField>()
        });
    }

    [HttpPost]
    public IActionResult Edit(FormMaster form)
    {
        if (!ModelState.IsValid)
            return View(new FormFillViewModel { Form = form });

        if (form.FormId == 0)
        {
            _formBuilderService.CreateForm(form);
            
        }
        else
        {
            _formBuilderService.UpdateForm(form);
        }

        return RedirectToAction("Index");
    }

    // 新增欄位
    [HttpGet]
    public IActionResult AddField(int formId)
    {
        return View(new FormField { FormId = formId });
    }

    [HttpPost]
    public IActionResult AddField(FormField field)
    {
        if (!ModelState.IsValid)
        {
            return View(field);
        }

        _formBuilderService.AddField(field);
        return RedirectToAction("Edit", new { id = field.FormId });
    }

    // 編輯欄位
    [HttpGet]
    public IActionResult EditField(int fieldId)
    {
        var field = _formBuilderService.GetFieldById(fieldId);
        return View(field);
    }

    [HttpPost]
    public IActionResult EditField(FormField field)
    {
        if (!ModelState.IsValid)
            return View(field);

        _formBuilderService.UpdateField(field);
        return RedirectToAction("Edit", new { id = field.FormId });
    }

    // 刪除欄位
    [HttpPost]
    public IActionResult DeleteField(int fieldId)
    {
        var field = _formBuilderService.GetFieldById(fieldId);
        _formBuilderService.DeleteField(fieldId);
        return RedirectToAction("Edit", new { id = field.FormId });
    }
    
}