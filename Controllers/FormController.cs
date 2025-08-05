using DynamicForm.Service.Interface;
using DynamicForm.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DynamicForm.Controllers;

/// <summary>
/// 表單主檔變更 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FormController : ControllerBase
{
    private readonly IFormService _formService;

    public FormController(IFormService formService)
    {
        _formService = formService;
    }
    
    [HttpGet]
    public IActionResult GetForms()
    {
        var vms = _formService.GetFormList();
        return Ok(vms);
    }
    
    /// <summary>
    /// 取得編輯/檢視/新增資料表單
    /// </summary>
    /// <param name="formId">FORM_FIELD_Master.ID</param>
    /// <param name="pk">資料主鍵，新增時可不傳</param>
    /// <returns>回傳填寫表單的畫面</returns>
    [HttpPost("{formId}")]
    public IActionResult GetForm(Guid formId, string? pk)
    {
        var vm = pk != null
            ? _formService.GetFormSubmission(formId, pk)
            : _formService.GetFormSubmission(formId);
        return Ok(vm);
    }
    
    [HttpPost]
    public IActionResult SubmitForm([FromBody] FormSubmissionInputModel input)
    {
        _formService.SubmitForm(input);
        return NoContent();
    }

}
