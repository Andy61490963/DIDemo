using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface.FormLogicInterface;

public interface IFormFieldConfigService
{
    List<FormFieldConfigDto> GetFormFieldConfig(Guid? id);
}