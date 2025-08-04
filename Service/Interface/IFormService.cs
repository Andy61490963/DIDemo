using DynamicForm.ViewModels;

namespace DynamicForm.Service.Interface;

public interface IFormService
{
    /// <summary>
    /// 取得 列表
    /// </summary>
    FormListDataViewModel GetFormList();
    
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