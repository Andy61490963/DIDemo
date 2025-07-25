using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using ClassLibrary;
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
        var ID = Guid.Parse("35EFA397-6C13-4FE5-A7BF-6EDAF2D3B73E");
        
        var res = _formService.GetFormSubmission(ID);
        return View(res);
    }
    
    [HttpPost]
    public IActionResult SubmitForm(string formName, Dictionary<Guid, string> fields)
    {
        // fields: key = FieldConfigId, value = 使用者填寫的值
        // 可存入 FORM_SUBMISSION + FORM_SUBMISSION_DATA

        return RedirectToAction("FormSubmitSuccess");
    }

}