using DynamicForm.Models;
using ClassLibrary;
using DynamicForm.Service.Service.FormLogicService;

namespace DynamicForm.Service.Interface.FormLogicInterface;

public interface IDropdownService
{
    List<FormDataRow> ToFormDataRows(
        IEnumerable<IDictionary<string, object?>> rawRows,
        string pkColumn,
        out List<object> rowIds);

    List<DropdownAnswerDto> GetAnswers(IEnumerable<object> rowIds);
    
    Dictionary<Guid, string> GetOptionTextMap(IEnumerable<DropdownAnswerDto> answers);

    void ReplaceDropdownIdsWithTexts(
        List<FormDataRow> rows,
        List<FormFieldConfigDto> fieldConfigs,
        List<DropdownAnswerDto> answers,
        Dictionary<Guid, string> optionTextMap);
}