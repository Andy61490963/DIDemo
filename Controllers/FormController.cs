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
    
    public IActionResult Get(Guid formId, Guid id)
    {
        var res = _formService.GetFormSubmission(formId, id);
        return View("Input", res);
    }

    
    [HttpPost]
    public IActionResult SubmitForm(Guid formId, Guid? rowId, Dictionary<Guid, string> userInputs)
    {
        _formService.SubmitForm(formId, rowId, userInputs);
        return RedirectToAction("FormSubmitSuccess");
    }

}