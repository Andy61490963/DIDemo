using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormDesignerService
{
    List<FormFieldViewModel> GetFieldsByTableName(string tableName);

    void UpdateField(FormFieldViewModel model);

    bool CheckFieldExists(Guid fieldId);
    
    List<FormFieldValidationRuleDto> GetValidationRulesByFieldId(Guid fieldId);

    void InsertValidationRule(FormFieldValidationRuleDto model);
    int GetNextValidationOrder(Guid fieldId);

    FormControlType GetControlTypeByFieldId(Guid fieldId);

    bool SaveValidationRule(FormFieldValidationRuleDto rule);
}