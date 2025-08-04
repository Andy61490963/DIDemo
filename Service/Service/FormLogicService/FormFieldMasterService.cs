using ClassLibrary;
using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using Microsoft.Data.SqlClient;

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
    
    public (FORM_FIELD_Master Master, List<string> SchemaColumns, List<FormFieldConfigDto> FieldConfigs) GetFormMetaAggregate(TableSchemaQueryType type)
    {
        var master = GetFormFieldMaster(type);
        if (master == null) return default; // 或 return (null, null, null);

        using var multi = _con.QueryMultiple(@"
        SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @type;
        SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @viewTable;
        SELECT ID, COLUMN_NAME, CONTROL_TYPE FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id;
    ", new { type = type.ToInt(), viewTable = master.VIEW_TABLE_NAME, id = master.BASE_TABLE_ID });

        var masterResult = multi.ReadFirstOrDefault<FORM_FIELD_Master>();
        var columns = multi.Read<string>().ToList();
        var configs = multi.Read<FormFieldConfigDto>().ToList();

        // tuple 直接 return
        return (masterResult, columns, configs);
    }

}