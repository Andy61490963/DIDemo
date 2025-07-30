using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service.FormLogicService;

public class FormFieldMasterService : IFormFieldMasterService
{
    private readonly SqlConnection _con;
    
    public FormFieldMasterService(SqlConnection connection)
    {
        _con = connection;
    }

    public FORM_FIELD_Master? GetFormFieldMaster(TableSchemaQueryType type)
    {
        return _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
            new { TYPE = type.ToInt() });
    }
    
    public FORM_FIELD_Master? GetFormFieldMasterFromId(Guid id)
    {
        return _con.QueryFirstOrDefault<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE ID = @id",
            new { id });
    }
}