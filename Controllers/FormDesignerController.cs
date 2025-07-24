using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DynamicForm.Controllers;

public class FormDesignerController : Controller
{
    private readonly IFormDesignerService _formDesignerService;

    public FormDesignerController(IFormDesignerService formDesignerService)
    {
        _formDesignerService = formDesignerService;
    }
    
    public IActionResult Index()
    {
        var model = new FormDesignerIndexViewModel
        {
            FormHeader = new FormHeaderViewModel(),
            FormField = new FormFieldListViewModel()
        };

        return View(model);
    }
    
    [HttpGet]
    public IActionResult QueryFields(string tableName)
    {
        FormFieldListViewModel result = _formDesignerService.GetFieldsByTableName(tableName);
        
        return PartialView("_FormFieldList", result);
    }
    
    [HttpGet]
    public IActionResult GetFieldSetting(string tableName, string columnName)
    {
        FormFieldViewModel? field = _formDesignerService.GetFieldsByTableName(tableName).Fields
                       .FirstOrDefault(x => x.COLUMN_NAME == columnName);
        
        return PartialView("_FormFieldSetting", field);
    }
    
    [HttpPost]
    public IActionResult UpdateFieldSetting(FormFieldViewModel model)
    {
        if (_formDesignerService.HasValidationRules(model.ID) &&
            _formDesignerService.GetControlTypeByFieldId(model.ID) != model.CONTROL_TYPE)
        {
            return Conflict("已有驗證規則，無法變更控制元件類型");
        }
        
        _formDesignerService.UpsertField(model);
        return Json(new { success = true });
    }

    [HttpGet]
    public IActionResult CheckFieldExists(Guid fieldId)
    {
        var exists = _formDesignerService.CheckFieldExists(fieldId);
        return Json(exists);
    }
    
    [HttpPost]
    public IActionResult SettingRule(Guid fieldId)
    {
        if (fieldId == Guid.Empty)
        {
            return BadRequest("請先設定控制元件後再新增驗證條件。");
        }
        
        ViewBag.ValidationTypeOptions = GetValidationTypeOptions(fieldId);

        List<FormFieldValidationRuleDto> rules = _formDesignerService.GetValidationRulesByFieldId(fieldId);
        return PartialView("SettingRule/_SettingRuleModal", rules);
    }
 
    [HttpPost]
    public IActionResult CreateEmptyValidationRule(Guid fieldConfigId)
    {
        ViewBag.ValidationTypeOptions = GetValidationTypeOptions(fieldConfigId);
        
        var newRule = _formDesignerService.CreateEmptyValidationRule(fieldConfigId);
        _formDesignerService.InsertValidationRule(newRule);
        
        List<FormFieldValidationRuleDto> rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);

        return PartialView("SettingRule/_ValidationRuleRow", rules);
    }

    
    [HttpPost]
    public IActionResult SaveValidationRule([FromBody] FormFieldValidationRuleDto rule)
    {
        _formDesignerService.SaveValidationRule(rule);
        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult DeleteValidationRule(Guid id, Guid fieldConfigId)
    {
        _formDesignerService.DeleteValidationRule(id);

        ViewBag.ValidationTypeOptions = GetValidationTypeOptions(fieldConfigId);

        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);
        return PartialView("SettingRule/_ValidationRuleRow", rules);
    }

    private List<SelectListItem> GetValidationTypeOptions(Guid fieldId)
    {
        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldId);
        var allowedValidations = ValidationRulesMap.GetValidations(controlType);
        return EnumExtensions.ToSelectList(allowedValidations);
    }
}