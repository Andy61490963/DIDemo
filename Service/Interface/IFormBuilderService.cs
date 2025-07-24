using DynamicForm.Models;

namespace DynamicForm.Service.Interface;

public interface IFormDesignerService
{
    List<FormFieldViewModel> GetFieldsByTableName(string tableName);

    void UpdateField(FormFieldViewModel model);

    bool CheckFieldExists(Guid fieldId);
    
    List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId);

    void InsertValidationRule(FormFieldValidationRuleDto model);
    int GetNextValidationOrder(Guid fieldId);

    bool SaveValidationRule(FormFieldValidationRuleDto rule);
}