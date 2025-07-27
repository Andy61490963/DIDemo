using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace DynamicForm.Controllers;

public class FormDesignerController : Controller
{
    private readonly IFormDesignerService _formDesignerService;
    private readonly IFormListService _formListService;

    public FormDesignerController(IFormDesignerService formDesignerService, IFormListService formListService)
    {
        _formDesignerService = formDesignerService;
        _formListService = formListService;
    }
    
    public IActionResult Index(Guid? id)
    {
        var model = new FormDesignerIndexViewModel
        {
            FormHeader = new FormHeaderViewModel(),
            FormField = new FormFieldListViewModel()
        };

        if (id.HasValue)
        {
            var master = _formListService.GetFormMaster(id.Value);
            if (master != null)
            {
                model.FormHeader = new FormHeaderViewModel
                {
                    ID = master.ID,
                    FORM_NAME = master.FORM_NAME,
                    TABLE_NAME = master.BASE_TABLE_NAME,
                    VIEW_TABLE_NAME = master.VIEW_TABLE_NAME,
                    PRIMARY_KEY = master.PRIMARY_KEY,
                    BASE_TABLE_ID = master.BASE_TABLE_ID,
                    VIEW_TABLE_ID = master.VIEW_TABLE_ID
                };

                var type = (TableSchemaQueryType)master.SCHEMA_TYPE;
                var fields = _formDesignerService.GetFieldsByTableName(master.BASE_TABLE_NAME, type);
                fields.ID = master.ID;
                fields.type = type;
                model.FormField = fields;
            }
        }

        return View(model);
    }
    
    [HttpGet]
    public IActionResult QueryFields(string tableName, TableSchemaQueryType schemaType)
    {
        FormFieldListViewModel result = _formDesignerService.EnsureFieldsSaved(tableName, schemaType);

        return PartialView("_FormFieldList", result);
    }
    
    [HttpGet]
    public IActionResult GetFieldSetting(string tableName, string columnName, TableSchemaQueryType schemaType)
    {
        FormFieldViewModel? field = _formDesignerService.GetFieldsByTableName(tableName, schemaType).Fields
                       .FirstOrDefault(x => x.COLUMN_NAME == columnName);
        if (field != null)
        {
            field.SchemaType = schemaType;
        }
        ViewBag.SchemaType = schemaType;
        return PartialView("_FormFieldSetting", field);
    }
    
    [HttpPost]
    public IActionResult UpdateFieldSetting(FormFieldViewModel model, TableSchemaQueryType schemaType)
    {
        // 1. 取得 FORM_FIELD_Master ID，如果不存在就新增
        var master = new FORM_FIELD_Master { ID = model.FORM_FIELD_Master_ID };
        var formMasterId = _formDesignerService.GetOrCreateFormMasterId(master);
        
        // 2. 驗證控制類型變更是否合法（不能改已有驗證規則的欄位）
        if (_formDesignerService.HasValidationRules(model.ID) &&
            _formDesignerService.GetControlTypeByFieldId(model.ID) != model.CONTROL_TYPE)
        {
            return Conflict("已有驗證規則，無法變更控制元件類型");
        }
        
        _formDesignerService.UpsertField(model, formMasterId);
        var fields = _formDesignerService.EnsureFieldsSaved(model.TableName, schemaType);
        fields.ID = formMasterId;
        fields.type = schemaType;
        return PartialView("_FormFieldList", fields);
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
    
    
    
    [HttpPost]
    public IActionResult DropdownSetting(Guid fieldId)
    {
        _formDesignerService.EnsureDropdownCreated(fieldId);
        var setting = _formDesignerService.GetDropdownSetting(fieldId);
        return PartialView("Dropdown/_DropdownModal", setting);
    }

    [HttpPost]
    public IActionResult SaveDropdownSql(Guid fieldId, string sql)
    {
        _formDesignerService.SaveDropdownSql(fieldId, sql);
        return Json(new { success = true });
    }

    // 新增空白選項
    [HttpPost]
    public IActionResult NewDropdownOption(Guid dropdownId)
    {
        Guid newId = _formDesignerService.SaveDropdownOption(null, dropdownId, "");
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return PartialView("Dropdown/_DropdownOptionItem", options);
    }

    // 編輯既有選項
    [HttpPost]
    public IActionResult SaveDropdownOption(Guid id, Guid dropdownId, string optionText)
    {
        _formDesignerService.SaveDropdownOption(id, dropdownId, optionText);
        return Json(new { success = true });
    }
    
    [HttpPost]
    public IActionResult DeleteOption(Guid optionId, Guid dropdownId)
    {
        _formDesignerService.DeleteDropdownOption(optionId);
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return PartialView("Dropdown/_DropdownOptionItem", options);
    }

    [HttpPost]
    public IActionResult SetDropdownMode(Guid dropdownId, bool isUseSql)
    {
        _formDesignerService.SetDropdownMode(dropdownId, isUseSql);
        return Json(new { success = true });
    }
    
    [HttpPost]
    public IActionResult ValidateDropdownSql(string sql)
    {
        var res = _formDesignerService.ValidateDropdownSql(sql);

        return PartialView("Dropdown/_ValidateSqlResult", res);
    }

    [HttpPost]
    public IActionResult SaveFormHeader([FromBody] FormHeaderViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TABLE_NAME))
        {
            return BadRequest("BASE_TABLE_NAME 不可為空");
        }

        if (string.IsNullOrWhiteSpace(model.VIEW_TABLE_NAME))
        {
            return BadRequest("VIEW_TABLE_NAME 不可為空");
        }

        var master = new FORM_FIELD_Master
        {
            ID = model.ID,
            FORM_NAME = model.FORM_NAME,
            BASE_TABLE_NAME = model.TABLE_NAME,
            VIEW_TABLE_NAME = model.VIEW_TABLE_NAME,
            BASE_TABLE_ID = model.BASE_TABLE_ID,
            VIEW_TABLE_ID = model.VIEW_TABLE_ID,
            PRIMARY_KEY = model.PRIMARY_KEY,
            STATUS = (int)TableStatusType.Active,
            SCHEMA_TYPE = (int)TableSchemaQueryType.All
        };

        var id = _formDesignerService.SaveFormHeader(master);

        return Json(new { success = true, id });
    }
    
    private List<SelectListItem> GetValidationTypeOptions(Guid fieldId)
    {
        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldId);
        var allowedValidations = ValidationRulesMap.GetValidations(controlType);
        return EnumExtensions.ToSelectList(allowedValidations);
    }
}