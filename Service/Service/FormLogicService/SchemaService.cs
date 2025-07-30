using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service.FormLogicService;

public class SchemaService : ISchemaService
{
    private readonly SqlConnection _con;
    
    public SchemaService(SqlConnection connection)
    {
        _con = connection;
    }

    public List<string> GetFormFieldMaster(string table)
    {
        return _con.Query<string>(
            "/**/SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table",
            new { table }).ToList();
    }
}