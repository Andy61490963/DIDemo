using DynamicForm.Models;
using ClassLibrary;

namespace DynamicForm.Service.Interface;

public interface IFormService
{
    FormSubmissionViewModel GetFormSubmission();
}