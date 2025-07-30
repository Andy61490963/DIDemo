using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers;

public class FormController : Controller
{
    private readonly IFormService _formService;

    public FormController(IFormService formService)
    {
        _formService = formService;
    }
    
    public IActionResult Index()
    {
        var vm = _formService.GetFormList();
        return View(vm);
    }
    
    /// <summary>
    /// 取得編輯/檢視/新增資料表單
    /// </summary>
    /// <param name="formId">FORM_FIELD_Master.ID</param>
    /// <param name="id">資料主鍵，新增時可不傳</param>
    /// <returns>回傳填寫表單的畫面</returns>
    [HttpGet]
    public IActionResult Input(Guid formId, Guid? id)
    {
        var vm = id.HasValue
            ? _formService.GetFormSubmission(formId, id.Value) // 編輯/檢視
            : _formService.GetFormSubmission(formId); // 新增
        return View("Input", vm);
    }
    
    [HttpPost]
    public IActionResult SubmitForm(FormSubmissionInputModel input)
    {
        _formService.SubmitForm(input);
        return RedirectToAction("FormSubmitSuccess");
    }

}