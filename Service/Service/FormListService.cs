using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.Helper;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormListService : IFormListService
{
    private readonly SqlConnection _con;
    
    public FormListService(SqlConnection connection)
    {
        _con = connection;
    }
    
    public List<FORM_FIELD_Master> GetFormMasters()
    {
        var statusList = new[] { TableStatusType.Active, TableStatusType.Disabled };
        return _con.Query<FORM_FIELD_Master>(Sql.FormMasterSelect, new{ STATUS = statusList }).ToList();
    }

    public FORM_FIELD_Master? GetFormMaster(Guid id)
    {
        return _con.QueryFirstOrDefault<FORM_FIELD_Master>(Sql.FormMasterById, new { id });
    }

    public void DeleteFormMaster(Guid id)
    {
        _con.Execute(Sql.DeleteFormMaster, new { id });
    }

    private static class Sql
    {
        public const string FormMasterSelect = @"SELECT M.*, BT.SOURCE_NAME AS BASE_TABLE_NAME, V.SOURCE_NAME AS VIEW_NAME
FROM FORM_FIELD_Master M
LEFT JOIN DATA_SOURCE_MASTER BT ON M.BASE_TABLE_ID = BT.ID
LEFT JOIN DATA_SOURCE_MASTER V  ON M.VIEW_ID = V.ID
WHERE M.STATUS IN @STATUS";

        public const string FormMasterById   = @"SELECT M.*, BT.SOURCE_NAME AS BASE_TABLE_NAME, V.SOURCE_NAME AS VIEW_NAME
FROM FORM_FIELD_Master M
LEFT JOIN DATA_SOURCE_MASTER BT ON M.BASE_TABLE_ID = BT.ID
LEFT JOIN DATA_SOURCE_MASTER V  ON M.VIEW_ID = V.ID
WHERE M.ID = @id";
        public const string DeleteFormMaster = @"
DELETE FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE FORM_FIELD_DROPDOWN_ID IN (
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
DELETE FROM FORM_FIELD_Master WHERE ID = @id;
";
    }
}