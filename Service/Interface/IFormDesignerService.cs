using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormDesignerService
{
    FormFieldListViewModel GetFieldsByTableName(string tableName);

    void UpsertField(FormFieldViewModel model);

    bool CheckFieldExists(Guid fieldId);
    
    List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId);

    bool HasValidationRules(Guid fieldId);

    FormFieldValidationRuleDto CreateEmptyValidationRule(Guid fieldConfigId);
    void InsertValidationRule(FormFieldValidationRuleDto model);
    int GetNextValidationOrder(Guid fieldId);

    FormControlType GetControlTypeByFieldId(Guid fieldId);

    bool SaveValidationRule(FormFieldValidationRuleDto rule);

    bool DeleteValidationRule(Guid id);
}