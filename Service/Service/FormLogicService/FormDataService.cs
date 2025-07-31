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

    public Dictionary<string, string> LoadColumnTypes(string tableName)
    {
        return _con.Query<(string COLUMN_NAME, string DATA_TYPE)>(
                @"/**/SELECT COLUMN_NAME, DATA_TYPE
          FROM INFORMATION_SCHEMA.COLUMNS
          WHERE TABLE_NAME = @TableName",
                new { TableName = tableName })
            .ToDictionary(x => x.COLUMN_NAME, x => x.DATA_TYPE, StringComparer.OrdinalIgnoreCase);
    }
}