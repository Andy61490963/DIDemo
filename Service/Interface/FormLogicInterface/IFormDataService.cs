using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface.FormLogicInterface;

public interface IFormDataService
{
    List<IDictionary<string, object?>> GetRows(string tableName);
}