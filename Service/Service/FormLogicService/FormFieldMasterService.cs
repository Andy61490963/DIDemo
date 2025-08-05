using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

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
    
    public FORM_FIELD_Master GetFormFieldMasterFromId(Guid id, SqlTransaction? tx = null)
    {
        return _con.QueryFirst<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE ID = @id",
            new { id }, transaction: tx);
    }
    
    public List<(FORM_FIELD_Master Master, List<string> SchemaColumns, List<FormFieldConfigDto> FieldConfigs)> GetFormMetaAggregates(TableSchemaQueryType type)
    {
        var masters = _con.Query<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
            new { TYPE = type.ToInt() })
            .ToList();

        var result = new List<(FORM_FIELD_Master, List<string>, List<FormFieldConfigDto>)>();

        foreach (var master in masters)
        {
            var columns = _con.Query<string>(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @viewTable",
                new { viewTable = master.VIEW_TABLE_NAME })
                .ToList();

            var configs = _con.Query<FormFieldConfigDto>(
                "SELECT ID, COLUMN_NAME, CONTROL_TYPE, CAN_QUERY FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
                new { id = master.BASE_TABLE_ID })
                .ToList();

            result.Add((master, columns, configs));
        }

        return result;
    }

}