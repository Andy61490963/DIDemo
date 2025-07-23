using DynamicForm.Models;

namespace DynamicForm.Service.Interface;

public interface IFormBuilderService
{
    List<FormMaster> GetAllForms();
    FormMaster GetFormById(int id);
    void CreateForm(FormMaster form);
    void UpdateForm(FormMaster form);

    List<FormField> GetFieldsByFormId(int formId);
    FormField GetFieldById(int fieldId);
    void AddField(FormField field);
    void UpdateField(FormField field);
    void DeleteField(int fieldId);

    void SaveResult(FormResult result);
}