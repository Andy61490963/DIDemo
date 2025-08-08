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

    public List<(FORM_FIELD_Master Master, List<FormFieldConfigDto> FieldConfigs)> GetFormMetaAggregates(TableSchemaQueryType type)
    {
        var masters = _con.Query<FORM_FIELD_Master>(
            "/**/SELECT * FROM FORM_FIELD_Master WHERE SCHEMA_TYPE = @TYPE",
            new { TYPE = type.ToInt() })
            .ToList();

        var result = new List<(FORM_FIELD_Master Master, List<FormFieldConfigDto> FieldConfigs)>();

        foreach (var master in masters)
        {
            var configs = _con.Query<FormFieldConfigDto>(
                "/**/SELECT ID, COLUMN_NAME, CONTROL_TYPE, CAN_QUERY FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @id",
                new { id = master.BASE_TABLE_ID })
                .ToList();

            result.Add((master, configs));
        }

        return result;
    }

    /// <summary>
    /// 從既有表單設定建立新的快照。
    /// </summary>
    /// <param name="sourceId">來源表單設定 ID。</param>
    /// <param name="tx">交易物件，可選。</param>
    /// <returns>新快照的 <see cref="FORM_FIELD_Master.ID"/>。</returns>
    public Guid CloneFormDefinition(Guid sourceId, SqlTransaction? tx = null)
    {
        var newMasterId = Guid.NewGuid();

        // 1. 複製 FORM_FIELD_Master
        _con.Execute(
            @"INSERT INTO FORM_FIELD_Master (ID, FORM_NAME, BASE_TABLE_NAME, VIEW_TABLE_NAME, BASE_TABLE_ID, VIEW_TABLE_ID, STATUS, SCHEMA_TYPE)
              SELECT @NewId, FORM_NAME, BASE_TABLE_NAME, VIEW_TABLE_NAME, BASE_TABLE_ID, VIEW_TABLE_ID, STATUS, SCHEMA_TYPE
              FROM FORM_FIELD_Master WHERE ID = @SourceId",
            new { NewId = newMasterId, SourceId = sourceId }, tx);

        // 2. 複製欄位設定並建立舊新 ID 對照
        var configMap = new Dictionary<Guid, Guid>();
        var configs = _con.Query<FormFieldConfigDto>(
            @"SELECT * FROM FORM_FIELD_CONFIG WHERE FORM_FIELD_Master_ID = @SourceId",
            new { SourceId = sourceId }, tx).ToList();

        foreach (var cfg in configs)
        {
            var newCfgId = Guid.NewGuid();
            configMap[cfg.ID] = newCfgId;
            _con.Execute(
                @"INSERT INTO FORM_FIELD_CONFIG (ID, FORM_FIELD_Master_ID, FORM_NAME, TABLE_NAME, SOURCE_TABLE, COLUMN_NAME, CONTROL_TYPE, QUERY_CONDITION_TYPE, QUERY_CONDITION_SQL, CAN_QUERY, DEFAULT_VALUE, IS_REQUIRED, IS_EDITABLE, FIELD_ORDER, DATA_TYPE)
                  VALUES (@Id, @MasterId, @FORM_NAME, @TABLE_NAME, @SOURCE_TABLE, @COLUMN_NAME, @CONTROL_TYPE, @QUERY_CONDITION_TYPE, @QUERY_CONDITION_SQL, @CAN_QUERY, @DEFAULT_VALUE, @IS_REQUIRED, @IS_EDITABLE, @FIELD_ORDER, @DATA_TYPE)",
                new
                {
                    Id = newCfgId,
                    MasterId = newMasterId,
                    cfg.FORM_NAME,
                    cfg.TABLE_NAME,
                    cfg.SOURCE_TABLE,
                    cfg.COLUMN_NAME,
                    cfg.CONTROL_TYPE,
                    cfg.QUERY_CONDITION_TYPE,
                    cfg.QUERY_CONDITION_SQL,
                    cfg.CAN_QUERY,
                    cfg.DEFAULT_VALUE,
                    cfg.IS_REQUIRED,
                    cfg.IS_EDITABLE,
                    cfg.FIELD_ORDER,
                    cfg.DATA_TYPE
                }, tx);
        }

        // 3. 複製下拉設定
        var dropdowns = _con.Query<FORM_FIELD_DROPDOWN>(
            @"SELECT * FROM FORM_FIELD_DROPDOWN WHERE FORM_FIELD_CONFIG_ID IN @Ids",
            new { Ids = configMap.Keys }, tx).ToList();
        var dropdownMap = new Dictionary<Guid, Guid>();
        foreach (var dd in dropdowns)
        {
            var newDdId = Guid.NewGuid();
            dropdownMap[dd.ID] = newDdId;
            _con.Execute(
                @"INSERT INTO FORM_FIELD_DROPDOWN (ID, FORM_FIELD_CONFIG_ID, ISUSESQL, DROPDOWNSQL)
                  VALUES (@Id, @ConfigId, @ISUSESQL, @DROPDOWNSQL)",
                new
                {
                    Id = newDdId,
                    ConfigId = configMap[dd.FORM_FIELD_CONFIG_ID],
                    dd.ISUSESQL,
                    dd.DROPDOWNSQL
                }, tx);
        }

        foreach (var kv in dropdownMap)
        {
            _con.Execute(
                @"INSERT INTO FORM_FIELD_DROPDOWN_OPTIONS (ID, FORM_FIELD_DROPDOWN_ID, OPTION_TABLE, OPTION_VALUE, OPTION_TEXT)
                  SELECT NEWID(), @NewDdId, OPTION_TABLE, OPTION_VALUE, OPTION_TEXT
                  FROM FORM_FIELD_DROPDOWN_OPTIONS WHERE FORM_FIELD_DROPDOWN_ID = @OldDdId",
                new { NewDdId = kv.Value, OldDdId = kv.Key }, tx);
        }

        // 4. 複製驗證規則
        var rules = _con.Query<FormFieldValidationRuleDto>(
            @"SELECT * FROM FORM_FIELD_VALIDATION_RULE WHERE FIELD_CONFIG_ID IN @Ids",
            new { Ids = configMap.Keys }, tx).ToList();
        foreach (var rule in rules)
        {
            _con.Execute(
                @"INSERT INTO FORM_FIELD_VALIDATION_RULE (ID, FIELD_CONFIG_ID, VALIDATION_TYPE, VALIDATION_VALUE, MESSAGE_ZH, MESSAGE_EN, VALIDATION_ORDER)
                  VALUES (NEWID(), @ConfigId, @VALIDATION_TYPE, @VALIDATION_VALUE, @MESSAGE_ZH, @MESSAGE_EN, @VALIDATION_ORDER)",
                new
                {
                    ConfigId = configMap[rule.FIELD_CONFIG_ID],
                    rule.VALIDATION_TYPE,
                    rule.VALIDATION_VALUE,
                    rule.MESSAGE_ZH,
                    rule.MESSAGE_EN,
                    rule.VALIDATION_ORDER
                }, tx);
        }

        return newMasterId;
    }
}

