namespace DynamicForm.Service.Interface;

using System.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
