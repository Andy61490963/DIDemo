using ClassLibrary;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DynamicForm.Controllers;

/// <summary>
/// 表單主檔詳細參數設定 API
/// </summary>
[ApiController]
[Route("api/form-designer")]
public class FormDesignerController : ControllerBase
{
    private readonly IFormDesignerService _formDesignerService;
    private readonly IFormListService     _formListService;

    public FormDesignerController(
        IFormDesignerService formDesignerService,
        IFormListService     formListService)
    {
        _formDesignerService = formDesignerService;
        _formListService     = formListService;
    }

    // ────────── Form Designer 入口 ──────────
    
    /// <summary>
    /// 取得指定表單的設計器主畫面資料。
    /// </summary>
    /// <param name="id">FORM_FIELD_Master 的唯一識別編號</param>
    [HttpPost("{id:guid}")]
    public IActionResult GetDesigner(Guid id)
    {
        var model = _formDesignerService.GetFormDesignerIndexViewModel(id);
        return Ok(model);
    }

    /// <summary>
    /// 查詢或建立指定資料表的欄位設定
    /// </summary>
    /// <param name="tableName">目標資料表名稱</param>
    /// <param name="schemaType">查詢類型（僅表格、僅 View、或全部）</param>
    [HttpPost("tables/{tableName}/fields")]
    public IActionResult QueryFields(string tableName, [FromQuery] TableSchemaQueryType schemaType)
    {
        var result = _formDesignerService.EnsureFieldsSaved(tableName, schemaType);
        return Ok(result);
    }

    /// <summary>
    /// 取得指定欄位的欄位設定資訊
    /// </summary>
    /// <param name="tableName">目標資料表名稱</param>
    /// <param name="columnName">目標欄位名稱</param>
    /// <param name="schemaType">查詢類型（僅表格、僅 View、或全部）</param>
    [HttpPost("tables/{tableName}/fields/{columnName}")]
    public IActionResult GetFieldSetting(string tableName, string columnName, [FromQuery] TableSchemaQueryType schemaType)
    {
        var field = _formDesignerService
                    .GetFieldsByTableName(tableName, schemaType)
                    .Fields.FirstOrDefault(x => x.COLUMN_NAME == columnName);
        return Ok(field);
    }

    /// <summary>
    /// 新增或更新單一欄位設定
    /// </summary>
    /// <param name="model">欄位設定 ViewModel</param>
    /// <param name="schemaType">查詢類型</param>
    [HttpPost("fields")]
    public IActionResult UpdateFieldSetting([FromBody] FormFieldViewModel model, [FromQuery] TableSchemaQueryType schemaType)
    {
        var master = new FORM_FIELD_Master { ID = model.FORM_FIELD_Master_ID };
        var formMasterId = _formDesignerService.GetOrCreateFormMasterId(master);

        if (model.ID != Guid.Empty &&
            _formDesignerService.HasValidationRules(model.ID) &&
            _formDesignerService.GetControlTypeByFieldId(model.ID) != model.CONTROL_TYPE)
            return Conflict("已有驗證規則，無法變更控制元件類型");

        _formDesignerService.UpsertField(model, formMasterId);
        var fields = _formDesignerService.EnsureFieldsSaved(model.TableName, schemaType);
        fields.ID            = formMasterId;
        fields.SchemaQueryType = schemaType;
        return Ok(fields);
    }

    // ────────── 批次設定 ──────────
    
    /// <summary>
    /// 設定指定資料表的所有欄位是否允許編輯
    /// </summary>
    /// <param name="formMasterId">表單主檔 ID</param>
    /// <param name="tableName">目標資料表名稱</param>
    /// <param name="isEditable">是否允許編輯</param>
    /// <param name="schemaType">查詢類型</param>
    [HttpPost("tables/{tableName}/editable")]
    public IActionResult SetAllEditable(Guid formMasterId, string tableName,
                                        [FromQuery] bool isEditable,
                                        [FromQuery] TableSchemaQueryType schemaType)
    {
        if (schemaType != TableSchemaQueryType.OnlyTable)
            return BadRequest("僅支援檢視欄位清單的批次設定。");

        _formDesignerService.SetAllEditable(formMasterId, tableName, isEditable);
        var fields = _formDesignerService.GetFieldsByTableName(tableName, schemaType);
        fields.ID            = formMasterId;
        fields.SchemaQueryType = schemaType;
        return Ok(fields);
    }

    /// <summary>
    /// 設定指定資料表的所有欄位是否為必填
    /// </summary>
    /// <param name="formMasterId">表單主檔 ID</param>
    /// <param name="tableName">目標資料表名稱</param>
    /// <param name="isRequired">是否設為必填</param>
    /// <param name="schemaType">查詢類型</param>
    [HttpPost("tables/{tableName}/required")]
    public IActionResult SetAllRequired(Guid formMasterId, string tableName,
                                        [FromQuery] bool isRequired,
                                        [FromQuery] TableSchemaQueryType schemaType)
    {
        if (schemaType != TableSchemaQueryType.OnlyTable)
            return BadRequest("僅支援檢視欄位清單的批次設定。");

        _formDesignerService.SetAllRequired(formMasterId, tableName, isRequired);
        var fields = _formDesignerService.GetFieldsByTableName(tableName, schemaType);
        fields.ID            = formMasterId;
        fields.SchemaQueryType = schemaType;
        return Ok(fields);
    }

    // ────────── 欄位驗證規則 ──────────
    
    /// <summary>
    /// 取得欄位的驗證規則與對應的驗證類型選項
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    [HttpPost("fields/{fieldId:guid}/rules")]
    public IActionResult SettingRule(Guid fieldId)
    {
        if (fieldId == Guid.Empty)
            return BadRequest("請先設定控制元件後再新增驗證條件。");

        var options = GetValidationTypeOptions(fieldId);
        var rules   = _formDesignerService.GetValidationRulesByFieldId(fieldId);
        return Ok(new { validationTypeOptions = options, rules });
    }

    /// <summary>
    /// 建立一筆空的驗證規則並回傳所有規則列表
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    [HttpPost("fields/{fieldId:guid}/rules/new")]
    public IActionResult CreateEmptyValidationRule(Guid fieldId)
    {
        var options = GetValidationTypeOptions(fieldId);
        var rule    = _formDesignerService.CreateEmptyValidationRule(fieldId);
        _formDesignerService.InsertValidationRule(rule);
        var rules = _formDesignerService.GetValidationRulesByFieldId(fieldId);
        return Ok(new { validationTypeOptions = options, rules });
    }

    /// <summary>
    /// 儲存單筆欄位驗證規則
    /// </summary>
    /// <param name="rule">驗證規則資料物件</param>
    [HttpPost("rules/{id:guid}")]
    public IActionResult SaveValidationRule([FromBody] FormFieldValidationRuleDto rule)
    {
        _formDesignerService.SaveValidationRule(rule);
        return Ok();
    }

    /// <summary>
    /// 刪除指定欄位驗證規則，並回傳最新的規則與驗證選項
    /// </summary>
    /// <param name="id">驗證規則 ID</param>
    /// <param name="fieldConfigId">欄位設定 ID</param>
    [HttpPost("rules/{id:guid}/delete")]
    public IActionResult DeleteValidationRule(Guid id, [FromQuery] Guid fieldConfigId)
    {
        _formDesignerService.DeleteValidationRule(id);
        var options = GetValidationTypeOptions(fieldConfigId);
        var rules   = _formDesignerService.GetValidationRulesByFieldId(fieldConfigId);
        return Ok(new { validationTypeOptions = options, rules });
    }

    // ────────── Dropdown ──────────
    
    /// <summary>
    /// 取得指定欄位的下拉選單設定，若尚未存在會自動建立
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    [HttpPost("fields/{fieldId:guid}/dropdown")]
    public IActionResult DropdownSetting(Guid fieldId)
    {
        _formDesignerService.EnsureDropdownCreated(fieldId);
        var setting = _formDesignerService.GetDropdownSetting(fieldId);
        return Ok(setting);
    }

    /// <summary>
    /// 設定下拉選單的資料來源模式（SQL或設定檔）
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    /// <param name="isUseSql">是否啟用 SQL 模式</param>
    [HttpPost("dropdowns/{dropdownId:guid}/mode")]
    public IActionResult SetDropdownMode(Guid dropdownId, [FromQuery] bool isUseSql)
    {
        _formDesignerService.SetDropdownMode(dropdownId, isUseSql);
        return Ok();
    }

    /// <summary>
    /// 儲存指定下拉選單的 SQL 查詢語法
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    /// <param name="sql">SQL 查詢語法</param>
    [HttpPost("dropdowns/{dropdownId:guid}/sql")]
    public IActionResult SaveDropdownSql(Guid dropdownId, [FromBody] string sql)
    {
        _formDesignerService.SaveDropdownSql(dropdownId, sql);
        return Ok();
    }

    /// <summary>
    /// 驗證 SQL 語法是否有效可執行，供下拉選單使用
    /// </summary>
    /// <param name="sql">SQL 查詢語法</param>
    [HttpPost("dropdowns/validate-sql")]
    public IActionResult ValidateDropdownSql([FromBody] string sql)
    {
        var res = _formDesignerService.ValidateDropdownSql(sql);
        return Ok(res);
    }

    /// <summary>
    /// 透過 SQL 查詢匯入下拉選單選項
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    /// <param name="dto">匯入選項所需資料（SQL 語法與來源表）</param>
    [HttpPost("dropdowns/{dropdownId:guid}/import")]
    public IActionResult ImportOptions(Guid dropdownId, [FromBody] ImportOptionDto dto)
    {
        var res = _formDesignerService.ImportDropdownOptionsFromSql(dto.Sql, dropdownId, dto.OptionTable);
        if (!res.Success) return BadRequest(res.Message);

        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return Ok(options);
    }

    /// <summary>
    /// 建立一筆新的空白下拉選項
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    [HttpPost("dropdowns/{dropdownId:guid}/options/new")]
    public IActionResult NewDropdownOption(Guid dropdownId)
    {
        _formDesignerService.SaveDropdownOption(null, dropdownId, "", "");
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return Ok(options);
    }

    /// <summary>
    /// 儲存單筆下拉選項資料（新增或更新）
    /// </summary>
    /// <param name="dropdownId">下拉選單 ID</param>
    /// <param name="dto">選項資料物件</param>
    [HttpPost("dropdowns/{dropdownId:guid}/options")]
    public IActionResult SaveDropdownOption(Guid dropdownId, [FromBody] SaveOptionDto dto)
    {
        _formDesignerService.SaveDropdownOption(dto.Id, dropdownId, dto.OptionText, dto.OptionValue);
        return Ok();
    }
    
    /// <summary>
    /// 刪除指定下拉選項，並回傳最新的選項列表
    /// </summary>
    /// <param name="optionId">選項 ID</param>
    /// <param name="dropdownId">所屬下拉選單 ID</param>
    [HttpPost("dropdowns/options/{optionId:guid}/delete")]
    public IActionResult DeleteOption(Guid optionId, [FromQuery] Guid dropdownId)
    {
        _formDesignerService.DeleteDropdownOption(optionId);
        var options = _formDesignerService.GetDropdownOptions(dropdownId);
        return Ok(options);
    }

    // ────────── Form Header ──────────
    
    /// <summary>
    /// 儲存表單主檔資訊（FORM_FIELD_Master），作為欄位設定的關聯基礎
    /// </summary>
    /// <param name="model">表單主檔 ViewModel</param>
    [HttpPost("headers")]
    public IActionResult SaveFormHeader([FromBody] FormHeaderViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TABLE_NAME) || string.IsNullOrWhiteSpace(model.VIEW_TABLE_NAME))
            return BadRequest("BASE_TABLE_NAME / VIEW_TABLE_NAME 不可為空");

        if (_formDesignerService.CheckFormMasterExists(model.TABLE_NAME, model.VIEW_TABLE_NAME, model.ID))
            return Conflict("相同的表格及 View 組合已存在");

        var master = new FORM_FIELD_Master
        {
            ID              = model.ID,
            FORM_NAME       = model.FORM_NAME,
            BASE_TABLE_NAME = model.TABLE_NAME,
            VIEW_TABLE_NAME = model.VIEW_TABLE_NAME,
            BASE_TABLE_ID   = model.BASE_TABLE_ID,
            VIEW_TABLE_ID   = model.VIEW_TABLE_ID,
            STATUS          = (int)TableStatusType.Active,
            SCHEMA_TYPE     = TableSchemaQueryType.All
        };

        var id = _formDesignerService.SaveFormHeader(master);
        return Ok(new { id });
    }

    // ────────── Util ──────────
    private List<SelectListItem> GetValidationTypeOptions(Guid fieldId)
    {
        var controlType = _formDesignerService.GetControlTypeByFieldId(fieldId);
        var allowed     = ValidationRulesMap.GetValidations(controlType);
        return EnumExtensions.ToSelectList(allowed);
    }
}
