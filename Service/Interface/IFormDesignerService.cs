using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormDesignerService
{
    Guid GetOrCreateFormMasterId(Guid id);
    FormFieldListViewModel GetFieldsByTableName(string tableName);

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
    Guid SaveDropdownOption(Guid? id, Guid dropdownId, string optionText);

    void DeleteDropdownOption(Guid optionId);

    void SetDropdownMode(Guid dropdownId, bool isUseSql);

    ValidateSqlResultViewModel ValidateDropdownSql(string sql);
    // void NewDropdownOption(Guid dropdownId);
}