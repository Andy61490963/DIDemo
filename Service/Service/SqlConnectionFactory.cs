using System.Data;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DynamicForm.Service.Service;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _config;

    public SqlConnectionFactory(IConfiguration config)
    {
        _config = config;
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqlConnection(_config.GetConnectionString("Connection"));
        return conn;
    }
}
