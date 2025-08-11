using System;
using System.Collections.Generic;
using System.Linq;
using DynamicForm.Areas.Form.Models;
using DynamicForm.Areas.Form.Services;
using DynamicForm.Areas.Form.Interfaces;
using DynamicForm.Areas.Form.Interfaces.FormLogic;
using DynamicForm.Areas.Form.ViewModels;
using Moq;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Tests.LogicTest;

public class FormServiceTests
{
    [Fact]
    public void GetFormList_FiltersConditionsByCanQuery()
    {
        var conn = new SqlConnection();
        var txSvc = new Mock<ITransactionService>();
        var masterSvc = new Mock<IFormFieldMasterService>();
        var schemaSvc = new Mock<ISchemaService>();
        var configSvc = new Mock<IFormFieldConfigService>();
        var dropdownSvc = new Mock<IDropdownService>();
        var dataSvc = new Mock<IFormDataService>();

        var master = new FORM_FIELD_Master
        {
            ID = Guid.NewGuid(),
            VIEW_TABLE_NAME = "V",
            VIEW_TABLE_ID = Guid.NewGuid(),
            BASE_TABLE_NAME = "B"
        };

        var fieldConfigs = new List<FormFieldConfigDto>
        {
            new FormFieldConfigDto
            {
                ID = Guid.NewGuid(),
                COLUMN_NAME = "Name",
                CONTROL_TYPE = FormControlType.Input,
                CAN_QUERY = true,
                IS_EDITABLE = true
            },
            new FormFieldConfigDto
            {
                ID = Guid.NewGuid(),
                COLUMN_NAME = "Age",
                CONTROL_TYPE = FormControlType.Input,
                CAN_QUERY = false,
                IS_EDITABLE = true
            }
        };

        masterSvc.Setup(s => s.GetFormMetaAggregates(TableSchemaQueryType.All))
            .Returns(new List<(FORM_FIELD_Master, List<string>, List<FormFieldConfigDto>)>
            {
                (master, new List<string> { "Name", "Age" }, fieldConfigs)
            });

        IEnumerable<FormQueryCondition>? passed = null;
        dataSvc.Setup(s => s.GetRows(master.VIEW_TABLE_NAME, It.IsAny<IEnumerable<FormQueryCondition>>()))
            .Callback<string, IEnumerable<FormQueryCondition>>((_, cond) => passed = cond?.ToList())
            .Returns(new List<IDictionary<string, object?>>());

        schemaSvc.Setup(s => s.GetPrimaryKeyColumn(master.BASE_TABLE_NAME)).Returns("ID");

        dropdownSvc.Setup(d => d.ToFormDataRows(It.IsAny<IEnumerable<IDictionary<string, object?>>>(), It.IsAny<string>(), out It.Ref<List<object>>.IsAny))
            .Returns((IEnumerable<IDictionary<string, object?>> rows, string pk, out List<object> rowIds) =>
            {
                rowIds = new List<object>();
                return new List<FormDataRow>();
            });
        dropdownSvc.Setup(d => d.GetAnswers(It.IsAny<IEnumerable<object>>())).Returns(new List<DropdownAnswerDto>());
        dropdownSvc.Setup(d => d.GetOptionTextMap(It.IsAny<IEnumerable<DropdownAnswerDto>>())).Returns(new Dictionary<Guid, string>());
        dropdownSvc.Setup(d => d.ReplaceDropdownIdsWithTexts(It.IsAny<List<FormDataRow>>(), It.IsAny<List<FormFieldConfigDto>>(), It.IsAny<List<DropdownAnswerDto>>(), It.IsAny<Dictionary<Guid, string>>()));

        configSvc.Setup(c => c.LoadFieldConfigData(master.VIEW_TABLE_ID))
            .Returns(new FieldConfigData(fieldConfigs, new List<FormFieldValidationRuleDto>(), new List<FORM_FIELD_DROPDOWN>(), new List<FORM_FIELD_DROPDOWN_OPTIONS>()));

        dataSvc.Setup(s => s.LoadColumnTypes(master.VIEW_TABLE_NAME))
            .Returns(new Dictionary<string, string> { { "Name", "nvarchar" }, { "Age", "int" } });

        var service = new FormService(conn, txSvc.Object, masterSvc.Object, schemaSvc.Object, configSvc.Object, dropdownSvc.Object, dataSvc.Object);

        var conditions = new List<FormQueryCondition>
        {
            new FormQueryCondition { Column = "Name", QueryConditionType = QueryConditionType.Text, DataType = "nvarchar", Value = "Alice" },
            new FormQueryCondition { Column = "Age", QueryConditionType = QueryConditionType.Text, DataType = "int", Value = "20" }
        };

        service.GetFormList(conditions);

        Assert.Single(passed);
        Assert.Equal("Name", passed!.First().Column);
    }
}
