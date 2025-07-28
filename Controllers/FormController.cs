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
        var ID = Guid.Parse("ECD95B96-1D13-4493-B42D-27C39619F79F");
        
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