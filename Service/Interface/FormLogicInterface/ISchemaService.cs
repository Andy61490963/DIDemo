using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface.FormLogicInterface;

public interface ISchemaService
{
    List<string> GetFormFieldMaster(string table);
}