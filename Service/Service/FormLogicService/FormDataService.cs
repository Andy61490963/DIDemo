using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service.FormLogicService;

public class FormDataService : IFormDataService
{
    private readonly SqlConnection _con;
    
    public FormDataService(SqlConnection connection)
    {
        _con = connection;
    }
    
    public List<IDictionary<string, object?>> GetRows(string tableName)
    {
        var rows = _con.Query($"SELECT * FROM [{tableName}]");
        return rows.Cast<IDictionary<string, object?>>().ToList();
    }

}