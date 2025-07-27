using DynamicForm.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicForm.Repository.Interface;

public interface IFormRepository
{
    List<FORM_FIELD_Master> GetFormMasters();
    Task<List<FORM_FIELD_Master>> GetFormMastersAsync();

    FORM_FIELD_Master? GetFormMaster(Guid id);
    Task<FORM_FIELD_Master?> GetFormMasterAsync(Guid id);

    void DeleteFormMaster(Guid id);
    Task DeleteFormMasterAsync(Guid id);
}
