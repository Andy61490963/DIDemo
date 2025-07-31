using DynamicForm.Models;
using ClassLibrary;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Interface.FormLogicInterface;

public interface ISchemaService
{
    List<string> GetFormFieldMaster(string table);

    (string PkName, string PkType, object? Value) ResolvePk(string tableName, string? rawId, SqlTransaction? tx = null);

    bool IsIdentityColumn(string tableName, string columnName, SqlTransaction? tx = null);
}