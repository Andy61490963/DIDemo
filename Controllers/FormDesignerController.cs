using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;

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
        if (string.IsNullOrEmpty(tableName))
        {
            return BadRequest("Table name is required.");
        }
        
        List<FormFieldViewModel> fields = _formDesignerService.GetFieldsByTableName(tableName);

        var result = new FormFieldListViewModel
        {
            TableName = tableName,
            Fields = fields
        };

        return PartialView("_FormFieldList", result);
    }
    
    [HttpGet]
    public IActionResult GetFieldSetting(string tableName, string columnName)
    {
        FormFieldViewModel? field = _formDesignerService.GetFieldsByTableName(tableName)
                       .FirstOrDefault(x => x.COLUMN_NAME == columnName);
        
        if (field == null)
        {
            return NotFound();
        }

        return PartialView("_FormFieldSetting", field);
    }
    
    [HttpPost]
    public IActionResult UpdateFieldSetting(FormFieldViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TableName) || string.IsNullOrWhiteSpace(model.COLUMN_NAME))
            return BadRequest("TableName 與 ColumnName 為必填");

        try
        {
            if (_formDesignerService.HasValidationRules(model.ID) &&
                _formDesignerService.GetControlTypeByFieldId(model.ID) != model.CONTROL_TYPE)
            {
                return Conflict("已有驗證規則，無法變更控制元件類型");
            }
            
            _formDesignerService.UpsertField(model);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            // TODO: log4net 紀錄 ex
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet]
    public IActionResult CheckFieldExists(Guid fieldId)
    {
        if (fieldId == Guid.Empty)
        {
            return Json(false);
        }

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

        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldId);
        var allowedValidations = ValidationRulesMap.GetValidations(controlType);
        ViewBag.ValidationTypeOptions = EnumExtensions.ToSelectList(allowedValidations);

        List<FormFieldValidationRuleDto> rules = _formDesignerService.GetValidationRulesByFieldId(fieldId);
        return PartialView("SettingRule/_SettingRuleModal", rules);
    }
 
    [HttpPost]
    public IActionResult CreateEmptyValidationRule(Guid fieldConfigId)
    {
        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldConfigId);
        var allowedValidations = ValidationRulesMap.GetValidations(controlType);
        ViewBag.ValidationTypeOptions = EnumExtensions.ToSelectList(allowedValidations);
        
        var newRule = new FormFieldValidationRuleDto
        {
            ID = Guid.NewGuid(),
            FIELD_CONFIG_ID = fieldConfigId,
            VALIDATION_TYPE = "",
            VALIDATION_VALUE = "",
            MESSAGE_ZH = "",
            MESSAGE_EN = "",
            VALIDATION_ORDER = _formDesignerService.GetNextValidationOrder(fieldConfigId)
        };

        _formDesignerService.InsertValidationRule(newRule);
        List<FormFieldValidationRuleDto> rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);

        return PartialView("SettingRule/_ValidationRuleRow", rules);
    }

    
    [HttpPost]
    public IActionResult SaveValidationRule([FromBody] FormFieldValidationRuleDto rule)
    {
        bool x = _formDesignerService.SaveValidationRule(rule);
        return Json(new { success = true });
    }

    [HttpPost]
    public IActionResult DeleteValidationRule(Guid id, Guid fieldConfigId)
    {
        _formDesignerService.DeleteValidationRule(id);

        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldConfigId);
        var allowedValidations = ValidationRulesMap.GetValidations(controlType);
        ViewBag.ValidationTypeOptions = EnumExtensions.ToSelectList(allowedValidations);

        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);
        return PartialView("SettingRule/_ValidationRuleRow", rules);
    }

}