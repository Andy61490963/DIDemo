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
}