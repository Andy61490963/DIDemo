using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormService
{
    FormSubmissionViewModel GetFormSubmission(Guid id, Guid? fromId = null);

    void SubmitForm(Guid formId, Guid? rowId, Dictionary<Guid, string> fields);
    
    /// <summary>
    /// 取得指定表單所對應檢視表的所有資料
    /// </summary>
    FormListDataViewModel GetFormList();
}