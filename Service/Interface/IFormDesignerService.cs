using DynamicForm.Models;
using ClassLibrary;
using DynamicForm.ViewModels;

namespace DynamicForm.Service.Interface;

public interface IFormDesignerService
{
    FormDesignerIndexViewModel GetFormDesignerIndexViewModel(Guid? id);
    Guid GetOrCreateFormMasterId(FORM_FIELD_Master model);
    FormFieldListViewModel? EnsureFieldsSaved(string tableName, TableSchemaQueryType type);
    FormFieldListViewModel GetFieldsByTableName(string tableName, TableSchemaQueryType schemaType);

    void UpsertField(FormFieldViewModel model, Guid formMasterId);

    /// <summary>
    /// 批次設定欄位的可編輯狀態。
    /// </summary>
    void SetAllEditable(Guid formMasterId, string tableName, bool isEditable);

    /// <summary>
    /// 批次設定欄位的必填狀態。
    /// </summary>
    void SetAllRequired(Guid formMasterId, string tableName, bool isRequired);

    bool CheckFieldExists(Guid fieldId);
    
    List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId);

    bool HasValidationRules(Guid fieldId);

    FormFieldValidationRuleDto CreateEmptyValidationRule(Guid fieldConfigId);
    void InsertValidationRule(FormFieldValidationRuleDto model);
    int GetNextValidationOrder(Guid fieldId);

    FormControlType GetControlTypeByFieldId(Guid fieldId);

    bool SaveValidationRule(FormFieldValidationRuleDto rule);

    bool DeleteValidationRule(Guid id);

    /// <summary>
    /// 確保 FORM_FIELD_DROPDOWN 存在，
    /// 可依需求指定預設的 SQL 來源與是否使用 SQL。
    /// </summary>
    /// <param name="fieldId">欄位設定 ID</param>
    /// <param name="isUseSql">是否使用 SQL 為資料來源，預設為 false；OnlyView 可帶入 null</param>
    /// <param name="sql">預設 SQL 查詢語句，預設為 null</param>
    void EnsureDropdownCreated(Guid fieldId, bool? isUseSql = false, string? sql = null);
    
    DropDownViewModel GetDropdownSetting(Guid fieldId);

    List<FORM_FIELD_DROPDOWN_OPTIONS> GetDropdownOptions(Guid dropDownId);
    
    void SaveDropdownSql(Guid fieldId, string sql);
    Guid SaveDropdownOption(Guid? id, Guid dropdownId, string optionText, string optionValue, string? optionTable = null);

    void DeleteDropdownOption(Guid optionId);

    void SetDropdownMode(Guid dropdownId, bool isUseSql);

    ValidateSqlResultViewModel ValidateDropdownSql(string sql);

    /// <summary>
    /// 執行 SQL 並將結果匯入指定的下拉選單選項表
    /// </summary>
    /// <param name="sql">要執行的查詢語法（僅限 SELECT）</param>
    /// <param name="dropdownId">目標下拉選單 ID</param>
    /// <returns>SQL 驗證與匯入結果</returns>
    ValidateSqlResultViewModel ImportDropdownOptionsFromSql(string sql, Guid dropdownId);
    Guid SaveFormHeader(FORM_FIELD_Master model);

    /// <summary>
    /// 檢查表格名稱與 View 名稱的組合是否已存在於 FORM_FIELD_Master
    /// </summary>
    /// <param name="baseTableName">資料表名稱</param>
    /// <param name="viewTableName">View 表名稱</param>
    /// <param name="excludeId">編輯時排除自身 ID</param>
    /// <returns>若存在相同組合則回傳 true</returns>
    bool CheckFormMasterExists(string baseTableName, string viewTableName, Guid? excludeId = null);
}