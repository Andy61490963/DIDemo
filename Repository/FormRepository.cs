using Dapper;
using DynamicForm.Models;
using DynamicForm.Repository.Interface;
using Microsoft.Data.SqlClient;
using ClassLibrary;

namespace DynamicForm.Repository;

public class FormRepository : IFormRepository
{
    private readonly SqlConnection _connection;

    public FormRepository(SqlConnection connection)
    {
        _connection = connection;
    }

    private const string FormMasterSelect = @"SELECT * FROM FORM_FIELD_Master WHERE STATUS IN @STATUS";
    private const string FormMasterById   = @"SELECT * FROM FORM_FIELD_Master WHERE ID = @id";
    private const string DeleteFormMaster = @"DELETE FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE FORM_FIELD_DROPDOWN_ID IN (
    SELECT ID FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID IN (
        SELECT ID FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id
    )
);
DELETE FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID IN (
    SELECT ID FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id
);
DELETE FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID IN (
    SELECT ID FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id
);
DELETE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id;
DELETE FROM FORM_FIELD_Master WHERE ID = @id;";

    public List<FORM_FIELD_Master> GetFormMasters()
    {
        var statusList = new[] { TableStatusType.Active, TableStatusType.Disabled };
        return _connection.Query<FORM_FIELD_Master>(FormMasterSelect, new { STATUS = statusList }).ToList();
    }

    public async Task<List<FORM_FIELD_Master>> GetFormMastersAsync()
    {
        var statusList = new[] { TableStatusType.Active, TableStatusType.Disabled };
        var result = await _connection.QueryAsync<FORM_FIELD_Master>(FormMasterSelect, new { STATUS = statusList });
        return result.ToList();
    }

    public FORM_FIELD_Master? GetFormMaster(Guid id)
    {
        return _connection.QueryFirstOrDefault<FORM_FIELD_Master>(FormMasterById, new { id });
    }

    public Task<FORM_FIELD_Master?> GetFormMasterAsync(Guid id)
    {
        return _connection.QueryFirstOrDefaultAsync<FORM_FIELD_Master>(FormMasterById, new { id });
    }

    public void DeleteFormMaster(Guid id)
    {
        _connection.Execute(DeleteFormMaster, new { id });
    }

    public Task DeleteFormMasterAsync(Guid id)
    {
        return _connection.ExecuteAsync(DeleteFormMaster, new { id });
    }
}
