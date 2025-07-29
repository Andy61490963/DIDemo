using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormDesignerService
{
    Guid GetOrCreateFormMasterId(FORM_FIELD_Master model);
    FormFieldListViewModel EnsureFieldsSaved(string tableName, TableSchemaQueryType type);
    FormFieldListViewModel GetFieldsByTableName(string tableName, TableSchemaQueryType schemaType);

    void UpsertField(FormFieldViewModel model, Guid formMasterId);

    bool CheckFieldExists(Guid fieldId);
    
    List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId);

    bool HasValidationRules(Guid fieldId);

    FormFieldValidationRuleDto CreateEmptyValidationRule(Guid fieldConfigId);
    void InsertValidationRule(FormFieldValidationRuleDto model);
    int GetNextValidationOrder(Guid fieldId);

    FormControlType GetControlTypeByFieldId(Guid fieldId);

    bool SaveValidationRule(FormFieldValidationRuleDto rule);

    bool DeleteValidationRule(Guid id);

    // Dropdown option related
    void EnsureDropdownCreated(Guid fieldId);
    
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
    /// <param name="optionTable">來源資料表名稱</param>
    /// <returns>SQL 驗證與匯入結果</returns>
    ValidateSqlResultViewModel ImportDropdownOptionsFromSql(string sql, Guid dropdownId, string optionTable);
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