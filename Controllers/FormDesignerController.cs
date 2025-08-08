using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using DynamicForm.Helper;

namespace DynamicForm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormDesignerController : ControllerBase
{
    private readonly IFormDesignerService _formDesignerService;
    private readonly IFormListService _formListService;

    public FormDesignerController(
        IFormDesignerService formDesignerService,
        IFormListService formListService)
    {
        _formDesignerService = formDesignerService;
        _formListService = formListService;
    }

    // ────────── Form Designer 入口 ──────────

    /// <summary>
    /// 取得指定表單的設計器主畫面資料
    /// </summary>
    [HttpGet("{id:guid}")]
    public IActionResult GetDesigner(Guid id)
    {
        var model = _formDesignerService.GetFormDesignerIndexViewModel(id);
        return Ok(model);
    }

    // ────────── 欄位相關 ──────────

    /// <summary>
    /// 取得資料表所有欄位設定
    /// </summary>
    [HttpGet("tables/{tableName}/fields")]
    public IActionResult GetFields(string tableName, [FromQuery] TableSchemaQueryType schemaType)
    {
        try
        {
            var result = _formDesignerService.EnsureFieldsSaved(tableName, schemaType);

            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        catch (HttpStatusCodeException ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }

    /// <summary>
    /// 依欄位設定 ID 取得單一欄位設定
    /// </summary>
    /// <param name="fieldId">欄位設定唯一識別碼</param>
    /// <param name="schemaType">資料表查詢類型</param>
    [HttpGet("fields/{fieldId}")]
    public IActionResult GetField(Guid fieldId, [FromQuery] TableSchemaQueryType schemaType)
    {
        var field = _formDesignerService.GetFieldById(fieldId);
        if (field == null) return NotFound();
        field.SchemaType = schemaType;
        return Ok(field);
    }

    /// <summary>
    /// 新增或更新單一欄位設定（ID 有值為更新，無值為新增）
    /// </summary>
    [HttpPost("fields")]
    public IActionResult UpsertField([FromBody] FormFieldViewModel model, [FromQuery] TableSchemaQueryType schemaType)
    {
        try
        {
            var ensure = _formDesignerService.EnsureFieldsSaved(model.TableName, schemaType);
            if (ensure == null)
            {
                return NotFound();
            }

            if (schemaType == TableSchemaQueryType.OnlyTable &&
                (model.QUERY_CONDITION_TYPE != QueryConditionType.None ||
                 model.QUERY_CONDITION_SQL != string.Empty ||
                 model.CAN_QUERY))
                return Conflict("無法往主表寫入查詢條件");
            
            if (model.ID != Guid.Empty &&
                _formDesignerService.HasValidationRules(model.ID) &&
                _formDesignerService.GetControlTypeByFieldId(model.ID) != model.CONTROL_TYPE)
                return Conflict("已有驗證規則，無法變更控制元件類型");

            var master = new FORM_FIELD_Master { ID = model.FORM_FIELD_Master_ID };
            var formMasterId = _formDesignerService.GetOrCreateFormMasterId(master);

            _formDesignerService.UpsertField(model, formMasterId);
            var fields = _formDesignerService.GetFieldsByTableName(model.TableName, schemaType);
            fields.ID = formMasterId;
            fields.SchemaQueryType = schemaType;
            return Ok(fields);
        }
        catch (HttpStatusCodeException ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }

    // ────────── 批次設定 ──────────

    /// <summary>
    /// 批次設定所有欄位為可編輯/不可編輯
    /// </summary>
    [HttpPost("tables/{tableName}/fields/batch-editable")]
    public IActionResult BatchSetEditable(
        [FromQuery] Guid formMasterId,
        string tableName,
        [FromQuery] bool isEditable,
        [FromQuery] TableSchemaQueryType schemaType)
    {
        if (schemaType != TableSchemaQueryType.OnlyTable)
            return BadRequest("僅支援檢視欄位清單的批次設定。");

        _formDesignerService.SetAllEditable(formMasterId, tableName, isEditable);
        var fields = _formDesignerService.GetFieldsByTableName(tableName, schemaType);
        fields.ID = formMasterId;
        fields.SchemaQueryType = schemaType;
        return Ok(fields);
    }

    /// <summary>
    /// 批次設定所有欄位為必填/非必填
    /// </summary>
    [HttpPost("tables/{tableName}/fields/batch-required")]
    public IActionResult BatchSetRequired(
        [FromQuery] Guid formMasterId,
        string tableName,
        [FromQuery] bool isRequired,
        [FromQuery] TableSchemaQueryType schemaType)
    {
        if (schemaType != TableSchemaQueryType.OnlyTable)
            return BadRequest("僅支援檢視欄位清單的批次設定。");

        _formDesignerService.SetAllRequired(formMasterId, tableName, isRequired);
        var fields = _formDesignerService.GetFieldsByTableName(tableName, schemaType);
        fields.ID = formMasterId;
        fields.SchemaQueryType = schemaType;
        return Ok(fields);
    }

    // ────────── 欄位驗證規則 ──────────

    /// <summary>
    /// 取得欄位驗證規則與驗證類型選項
    /// </summary>
    [HttpGet("fields/{fieldId:guid}/rules")]
    public IActionResult GetValidationRules(Guid fieldId)
    {
        if (fieldId == Guid.Empty)
            return BadRequest("請先設定控制元件後再新增驗證條件。");

        var options = GetValidationTypeOptions(fieldId);
        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldId);
        return Ok(new { validationTypeOptions = options, rules });
    }

    /// <summary>
    /// 新增一筆空的驗證規則並回傳全部規則
    /// </summary>
    [HttpPost("fields/{fieldId:guid}/rules")]
    public IActionResult AddEmptyValidationRule(Guid fieldId)
    {
        var options = GetValidationTypeOptions(fieldId);
        var rule = _formDesignerService.CreateEmptyValidationRule(fieldId);
        _formDesignerService.InsertValidationRule(rule);
        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldId);
        return Ok(new { validationTypeOptions = options, rules });
    }

    /// <summary>
    /// 更新單一驗證規則
    /// </summary>
    [HttpPut("rules/{id:guid}")]
    public IActionResult UpdateValidationRule([FromBody] FormFieldValidationRuleDto rule)
    {
        _formDesignerService.SaveValidationRule(rule);
        return Ok();
    }

    /// <summary>
    /// 刪除驗證規則
    /// </summary>
    [HttpDelete("rules/{id:guid}")]
    public IActionResult DeleteValidationRule(Guid id, [FromQuery] Guid fieldConfigId)
    {
        _formDesignerService.DeleteValidationRule(id);
        var options = GetValidationTypeOptions(fieldConfigId);
        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);
        return Ok(new { validationTypeOptions = options, rules });
    }

    // ────────── Dropdown ──────────

    /// <summary>
    /// 取得下拉選單設定（不存在則自動建立）
    /// </summary>
    [HttpGet("fields/{fieldId:guid}/dropdown")]
    public IActionResult GetDropdownSetting(Guid fieldId)
    {
        _formDesignerService.EnsureDropdownCreated(fieldId);
        var setting = _formDesignerService.GetDropdownSetting(fieldId);
        return Ok(setting);
    }

    /// <summary>
    /// 設定下拉選單資料來源模式（SQL/設定檔）
    /// </summary>
    [HttpPut("dropdowns/{dropdownId:guid}/mode")]
    public IActionResult SetDropdownMode(Guid dropdownId, [FromQuery] bool isUseSql)
    {
        _formDesignerService.SetDropdownMode(dropdownId, isUseSql);
        return Ok();
    }

    /// <summary>
    /// 儲存下拉選單 SQL 查詢
    /// </summary>
    [HttpPut("dropdowns/{dropdownId:guid}/sql")]
    public IActionResult SaveDropdownSql(Guid dropdownId, [FromBody] string sql)
    {
        _formDesignerService.SaveDropdownSql(dropdownId, sql);
        return Ok();
    }

    /// <summary>
    /// 驗證下拉 SQL 語法
    /// </summary>
    [HttpPost("dropdowns/validate-sql")]
    public IActionResult ValidateDropdownSql([FromBody] string sql)
    {
        var res = _formDesignerService.ValidateDropdownSql(sql);
        return Ok(res);
    }

    /// <summary>
    /// 匯入下拉選單選項（由 SQL 查詢）
    /// </summary>
    [HttpPost("dropdowns/{dropdownId:guid}/import-options")]
    public IActionResult ImportDropdownOptions(Guid dropdownId, [FromBody] ImportOptionDto dto)
    {
        var res = _formDesignerService.ImportDropdownOptionsFromSql(dto.Sql, dropdownId);
        if (!res.Success) return BadRequest(res.Message);

        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return Ok(options);
    }

    /// <summary>
    /// 建立一筆空白下拉選項
    /// </summary>
    [HttpPost("dropdowns/{dropdownId:guid}/options")]
    public IActionResult CreateDropdownOption(Guid dropdownId)
    {
        _formDesignerService.SaveDropdownOption(null, dropdownId, "", "");
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return Ok(options);
    }

    /// <summary>
    /// 儲存單筆下拉選項（新增/更新）
    /// </summary>
    [HttpPut("dropdowns/{dropdownId:guid}/options")]
    public IActionResult SaveDropdownOption(Guid dropdownId, [FromBody] SaveOptionDto dto)
    {
        _formDesignerService.SaveDropdownOption(dto.Id, dropdownId, dto.OptionText, dto.OptionValue);
        return Ok();
    }

    /// <summary>
    /// 刪除下拉選項
    /// </summary>
    [HttpDelete("dropdowns/options/{optionId:guid}")]
    public IActionResult DeleteDropdownOption(Guid optionId, [FromQuery] Guid dropdownId)
    {
        _formDesignerService.DeleteDropdownOption(optionId);
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return Ok(options);
    }

    // ────────── Form Header ──────────

    /// <summary>
    /// 儲存表單主檔資訊
    /// </summary>
    [HttpPost("headers")]
    public IActionResult SaveFormHeader([FromBody] FormHeaderViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TABLE_NAME) || string.IsNullOrWhiteSpace(model.VIEW_TABLE_NAME))
            return BadRequest("BASE_TABLE_NAME / VIEW_TABLE_NAME 不可為空");

        if (_formDesignerService.CheckFormMasterExists(model.TABLE_NAME, model.VIEW_TABLE_NAME, model.ID))
            return Conflict("相同的表格及 View 組合已存在");

        var master = new FORM_FIELD_Master
        {
            ID = model.ID,
            FORM_NAME = model.FORM_NAME,
            BASE_TABLE_NAME = model.TABLE_NAME,
            VIEW_TABLE_NAME = model.VIEW_TABLE_NAME,
            BASE_TABLE_ID = model.BASE_TABLE_ID,
            VIEW_TABLE_ID = model.VIEW_TABLE_ID,
            STATUS = (int)TableStatusType.Active,
            SCHEMA_TYPE = TableSchemaQueryType.All
        };

        var id = _formDesignerService.SaveFormHeader(master);
        return Ok(new { id });
    }

    // ────────── Util ──────────
    private List<ValidationTypeOptionDto> GetValidationTypeOptions(Guid fieldId)
    {
        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldId);
        var allowed = ValidationRulesMap.GetValidations(controlType);
        return EnumExtensions.ToSelectList(allowed);
    }
}