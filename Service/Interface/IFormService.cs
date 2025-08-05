using DynamicForm.ViewModels;
using System.Collections.Generic;

namespace DynamicForm.Service.Interface;

public interface IFormService
{
    /// <summary>
    /// 取得所有表單的資料列表。
    /// </summary>
    /// <returns>每個表單對應的欄位與資料列集合。</returns>
    List<FormListDataViewModel> GetFormList();
    
    /// <summary>
    /// 取得 單一
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pk"></param>
    /// <returns></returns>
    FormSubmissionViewModel GetFormSubmission(Guid id, string? pk = null);

    /// <summary>
    /// 儲存或更新表單資料
    /// </summary>
    void SubmitForm(FormSubmissionInputModel input);
}