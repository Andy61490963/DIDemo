using DynamicForm.Models;
using ClassLibrary;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace DynamicForm.Service.Interface.FormLogicInterface;

public interface IFormFieldMasterService
{
    FORM_FIELD_Master? GetFormFieldMaster(TableSchemaQueryType type);

    FORM_FIELD_Master GetFormFieldMasterFromId(Guid id, SqlTransaction? tx = null );

    List<(FORM_FIELD_Master Master, List<FormFieldConfigDto> FieldConfigs)> GetFormMetaAggregates(
        TableSchemaQueryType type);

    /// <summary>
    /// 建立指定表單設定的獨立快照，並回傳新快照的 <see cref="FORM_FIELD_Master.ID"/>。
    /// </summary>
    /// <param name="sourceId">來源表單設定 ID。</param>
    /// <param name="tx">交易物件，可選。</param>
    /// <returns>新快照的 ID。</returns>
    Guid CloneFormDefinition(Guid sourceId, SqlTransaction? tx = null);
}