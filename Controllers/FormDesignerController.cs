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
    
    /// <summary>
    /// 顯示表單設計器主畫面，包含表單主檔與欄位設定資訊。
    /// </summary>
    /// <param name="id">FORM_FIELD_Master 的唯一識別編號</param>
    public IActionResult Index(Guid? id)
    {
        var model = new FormDesignerIndexViewModel
        {
            FormHeader = new FormHeaderViewModel(),
            BaseFields = new FormFieldListViewModel(),
            ViewFields = new FormFieldListViewModel(),
            FieldSetting = new FormFieldViewModel()
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

                // 主表欄位
                var baseFields = _formDesignerService.GetFieldsByTableName(master.BASE_TABLE_NAME, TableSchemaQueryType.OnlyTable);
                baseFields.ID = master.ID;
                baseFields.type = TableSchemaQueryType.OnlyTable;
                model.BaseFields = baseFields;

                // View 欄位
                if (!string.IsNullOrWhiteSpace(master.VIEW_TABLE_NAME))
                {
                    var viewFields = _formDesignerService.GetFieldsByTableName(master.VIEW_TABLE_NAME, TableSchemaQueryType.OnlyView);
                    viewFields.ID = master.ID;
                    viewFields.type = TableSchemaQueryType.OnlyView;
                    model.ViewFields = viewFields;
                }
            }
        }

        return View(model);
    }
    
    /// <summary>
    /// 查詢指定資料表的欄位設定，若尚未儲存則自動建立後回傳。
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <param name="schemaType">查詢類型（OnlyTable / OnlyView / All）</param>
    [HttpGet]
    public IActionResult QueryFields(string tableName, TableSchemaQueryType schemaType)
    {
        FormFieldListViewModel result = _formDesignerService.EnsureFieldsSaved(tableName, schemaType);

        return PartialView("_FormFieldList", result);
    }
    
    /// <summary>
    /// 取得指定欄位的欄位設定詳細資訊，用於右側設定區塊顯示。
    /// </summary>
    /// <param name="tableName">資料表名稱</param>
    /// <param name="columnName">欄位名稱</param>
    /// <param name="schemaType">查詢類型</param>
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
    
    /// <summary>
    /// 儲存欄位設定，包含新增或更新邏輯，並重新回傳欄位列表 PartialView。
    /// </summary>
    /// <param name="model">欄位設定 ViewModel</param>
    /// <param name="schemaType">查詢類型</param>
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

    /// <summary>
    /// 檢查指定欄位 ID 是否存在於資料庫中(要先有控制元件，才能新增限制條件)
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
    [HttpGet]
    public IActionResult CheckFieldExists(Guid fieldId)
    {
        var exists = _formDesignerService.CheckFieldExists(fieldId);
        return Json(exists);
    }
    
    /// <summary>
    /// 顯示設定欄位驗證規則的 Modal 畫面。
    /// </summary>
    /// <param name="fieldId">欄位唯一識別碼</param>
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
 
    /// <summary>
    /// 建立一筆空白的驗證規則並儲存，回傳部分檢視更新畫面。
    /// </summary>
    /// <param name="fieldConfigId">欄位設定 ID</param>
    [HttpPost]
    public IActionResult CreateEmptyValidationRule(Guid fieldConfigId)
    {
        ViewBag.ValidationTypeOptions = GetValidationTypeOptions(fieldConfigId);
        
        var newRule = _formDesignerService.CreateEmptyValidationRule(fieldConfigId);
        _formDesignerService.InsertValidationRule(newRule);
        
        List<FormFieldValidationRuleDto> rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);

        return PartialView("SettingRule/_ValidationRuleRow", rules);
    }

    /// <summary>
    /// 儲存驗證規則資訊。
    /// </summary>
    /// <param name="rule">驗證規則 DTO</param>
    [HttpPost]
    public IActionResult SaveValidationRule([FromBody] FormFieldValidationRuleDto rule)
    {
        _formDesignerService.SaveValidationRule(rule);
        return Json(new { success = true });
    }

    /// <summary>
    /// 刪除指定驗證規則，並回傳更新後的規則列表 PartialView。
    /// </summary>
    /// <param name="id">驗證規則 ID</param>
    /// <param name="fieldConfigId">欄位設定 ID</param>
    [HttpPost]
    public IActionResult DeleteValidationRule(Guid id, Guid fieldConfigId)
    {
        _formDesignerService.DeleteValidationRule(id);

        ViewBag.ValidationTypeOptions = GetValidationTypeOptions(fieldConfigId);

        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);
        return PartialView("SettingRule/_ValidationRuleRow", rules);
    }
    
    /// <summary>
    /// 開啟下拉選項設定 Modal，若尚未建立則自動建立對應的 Dropdown 設定。
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    [HttpPost]
    public IActionResult DropdownSetting(Guid fieldId)
    {
        _formDesignerService.EnsureDropdownCreated(fieldId);
        var setting = _formDesignerService.GetDropdownSetting(fieldId);
        return PartialView("Dropdown/_DropdownModal", setting);
    }

    /// <summary>
    /// 儲存 SQL 型下拉選單的資料來源語法。
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    /// <param name="sql">SQL 查詢語句</param>
    [HttpPost]
    public IActionResult SaveDropdownSql(Guid fieldId, string sql)
    {
        _formDesignerService.SaveDropdownSql(fieldId, sql);
        return Json(new { success = true });
    }

    /// <summary>
    /// 新增一筆空白下拉選項，並回傳更新後的下拉選項 PartialView。
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    [HttpPost]
    public IActionResult NewDropdownOption(Guid dropdownId)
    {
        Guid newId = _formDesignerService.SaveDropdownOption(null, dropdownId, "", "", "");
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return PartialView("Dropdown/_DropdownOptionItem", options);
    }

    /// <summary>
    /// 儲存指定的下拉選項文字。
    /// </summary>
    /// <param name="id">選項 ID，若為 null 表示新增</param>
    /// <param name="dropdownId">所屬下拉選單 ID</param>
    /// <param name="optionText">選項文字內容</param>
    [HttpPost]
    public IActionResult SaveDropdownOption(Guid id, Guid dropdownId, string optionText, string optionValue, string optionTable)
    {
        _formDesignerService.SaveDropdownOption(id, dropdownId, optionText, optionValue, optionTable);
        return Json(new { success = true });
    }
    
    /// <summary>
    /// 刪除指定的下拉選項，並回傳更新後的選項 PartialView。
    /// </summary>
    /// <param name="optionId">選項 ID</param>
    /// <param name="dropdownId">所屬下拉選單 ID</param>
    [HttpPost]
    public IActionResult DeleteOption(Guid optionId, Guid dropdownId)
    {
        _formDesignerService.DeleteDropdownOption(optionId);
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return PartialView("Dropdown/_DropdownOptionItem", options);
    }

    /// <summary>
    /// 設定下拉選單的模式（靜態或 SQL 模式）。
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    /// <param name="isUseSql">是否啟用 SQL 模式</param>
    [HttpPost]
    public IActionResult SetDropdownMode(Guid dropdownId, bool isUseSql)
    {
        _formDesignerService.SetDropdownMode(dropdownId, isUseSql);
        return Json(new { success = true });
    }
    
    /// <summary>
    /// 驗證使用者輸入的 SQL 是否能正確執行，並回傳驗證結果 PartialView。
    /// </summary>
    /// <param name="sql">使用者輸入的 SQL 語法</param>
    [HttpPost]
    public IActionResult ValidateDropdownSql(string sql)
    {
        var res = _formDesignerService.ValidateDropdownSql(sql);

        return PartialView("Dropdown/_ValidateSqlResult", res);
    }

    /// <summary>
    /// 執行 SQL 並匯入下拉選單選項
    /// </summary>
    /// <param name="dropdownId">目標下拉選單 ID</param>
    /// <param name="sql">查詢語句</param>
    /// <param name="optionTable">來源表名稱</param>
    [HttpPost]
    public IActionResult ImportOptions(Guid dropdownId, string sql, string optionTable)
    {
        var res = _formDesignerService.ImportDropdownOptionsFromSql(sql, dropdownId, optionTable);
        if (!res.Success)
        {
            return BadRequest(res.Message);
        }

        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return PartialView("Dropdown/_DropdownOptionItem", options);
    }

    /// <summary>
    /// 儲存表單主檔資訊（FormHeader），作為欄位設定的對應關聯。
    /// </summary>
    /// <param name="model">表單主檔 ViewModel</param>
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

        // 檢查表格名稱與 View 名稱組合是否重複
        if (_formDesignerService.CheckFormMasterExists(model.TABLE_NAME, model.VIEW_TABLE_NAME, model.ID))
        {
            return Conflict("相同的表格及 View 組合已存在");
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
    
    /// <summary>
    /// 取得指定欄位控制型態對應的驗證規則選項清單(共用)
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    private List<SelectListItem> GetValidationTypeOptions(Guid fieldId)
    {
        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldId);
        var allowedValidations = ValidationRulesMap.GetValidations(controlType);
        return EnumExtensions.ToSelectList(allowedValidations);
    }
}