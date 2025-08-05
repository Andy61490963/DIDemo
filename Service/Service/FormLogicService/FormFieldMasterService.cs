using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.ViewModels;
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
    
    public List<(FORM_FIELD_Master Master, List<FormFieldInputViewModel> FieldConfigRows)> GetFormMetaAggregates(TableSchemaQueryType type)
    {
        var masters = _con.Query<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
            new { TYPE = type.ToInt() })
            .ToList();

        var result = new List<(FORM_FIELD_Master, List<FormFieldInputViewModel>)>();

        foreach (var master in masters)
        {
            var configs = _con.Query<FormFieldInputViewModel>(
                @"SELECT ID AS FieldConfigId,
                        COLUMN_NAME AS [Column],
                        DATA_TYPE,
                        CONTROL_TYPE,
                        DEFAULT_VALUE AS DefaultValue,
                        IS_REQUIRED,
                        IS_EDITABLE,
                        ISUSESQL,
                        DROPDOWNSQL
                  FROM FORM_FIELD_CONFIG
                 WHERE FORM_FIELD_Master_ID = @id AND SCHEMA_TYPE = @schemaType",
                new { id = master.VIEW_TABLE_ID, schemaType = TableSchemaQueryType.OnlyView.ToInt() })
                .ToList();

            result.Add((master, configs));
        }

        return result;
    }

}