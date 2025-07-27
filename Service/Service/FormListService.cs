using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Repository.Interface;
using System.Collections.Generic;

namespace DynamicForm.Service.Service;

public class FormListService : IFormListService
{
    private readonly IFormRepository _repository;

    public FormListService(IFormRepository repository)
    {
        _repository = repository;
    }
    
    public List<FORM_FIELD_Master> GetFormMasters()
    {
        return _repository.GetFormMasters();
    }

    public FORM_FIELD_Master? GetFormMaster(Guid id)
    {
        return _repository.GetFormMaster(id);
    }

    public void DeleteFormMaster(Guid id)
    {
        _repository.DeleteFormMaster(id);
    }

}
