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
    
    /// <summary>
    /// 根據指定的 SCHEMA_TYPE，取得所有表單主設定（FORM_FIELD_Master），
    /// 並載入其對應的欄位設定（FORM_FIELD_CONFIG）。
    /// </summary>
    /// <param name="type">欲查詢的 SCHEMA_TYPE（主表、View 或 All）</param>
    /// <returns>
    /// 回傳每筆表單主設定及其對應欄位設定清單。
    /// </returns>
    public List<(FORM_FIELD_Master Master, List<FormFieldConfigDto> FieldConfigs)> GetFormMetaAggregates(TableSchemaQueryType type)
    {
        // 1. 根據 SCHEMA_TYPE 查詢表單主設定（FORM_FIELD_Master）
        var masters = _con.Query<FORM_FIELD_Master>(
                @"/**/
SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
                new { TYPE = type.ToInt() })
            .ToList();

        // 2. 建立結果容器（每筆包含 Master 設定與欄位設定）
        var result = new List<(FORM_FIELD_Master Master, List<FormFieldConfigDto> FieldConfigs)>();

        // 3. 對每筆 master 查出對應欄位設定（FORM_FIELD_CONFIG）
        foreach (var master in masters)
        {
            var configs = _con.Query<FormFieldConfigDto>(
                    @"/**/
SELECT ID, COLUMN_NAME, CONTROL_TYPE, CAN_QUERY 
FROM FORM_FIELD_CONFIG 
WHERE FORM_FIELD_Master_ID = @id",
                    new { id = master.BASE_TABLE_ID })
                .ToList();

            result.Add((master, configs));
        }

        // 4. 回傳整理後資料
        return result;
    }

}