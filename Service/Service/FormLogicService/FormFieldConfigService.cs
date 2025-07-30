using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service.FormLogicService;

public class FormFieldConfigService : IFormFieldConfigService
{
    private readonly SqlConnection _con;
    
    public FormFieldConfigService(SqlConnection connection)
    {
        _con = connection;
    }

    public List<FormFieldConfigDto> GetFormFieldConfig(Guid? id)
    {
        return _con.Query<FormFieldConfigDto>(
            "/**/SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
            new { id }).ToList();
    }
}