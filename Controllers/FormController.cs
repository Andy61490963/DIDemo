﻿using ClassLibrary;
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
    
    public IActionResult Get(Guid id)
    {
        var res = _formService.GetFormSubmission(id);
        return View("Input",res);
    }
    
    [HttpPost]
    public IActionResult SubmitForm(string formName, Dictionary<Guid, string> fields)
    {
        // fields: key = FieldConfigId, value = 使用者填寫的值
        // 可存入 FORM_SUBMISSION + FORM_SUBMISSION_DATA

        return RedirectToAction("FormSubmitSuccess");
    }

}