using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormListService
{
    List<FORM_FIELD_Master> GetFormMasters();
    FORM_FIELD_Master? GetFormMaster(Guid id);
    void DeleteFormMaster(Guid id);
}