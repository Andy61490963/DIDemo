using DynamicForm.ViewModels;
using System.Collections.Generic;

namespace DynamicForm.Service.Interface;

public interface IFormService
{
    /// <summary>
    /// 取得資料列表，支援多個表單結果
    /// </summary>
    /// <returns>包含多個表單資料的集合</returns>
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
