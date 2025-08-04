using ClassLibrary;
using DynamicForm.Controllers;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using DynamicForm.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DynamicForm.Tests;

/// <summary>
/// 測試 <see cref="FormDesignerController"/> 主要的 API 行為。
/// 透過模擬的服務層確保 Controller 的邏輯正確。
/// </summary>
public class FormDesignerControllerTests
{
    private readonly Mock<IFormDesignerService> _designerMock = new();
    private readonly Mock<IFormListService>     _listMock     = new();

    private FormDesignerController CreateController()
        => new FormDesignerController(_designerMock.Object, _listMock.Object);

    [Fact]
    public void GetDesigner_ReturnsViewModel()
    {
        var id = Guid.NewGuid();
        var vm = new FormDesignerIndexViewModel();
        _designerMock.Setup(s => s.GetFormDesignerIndexViewModel(id)).Returns(vm);
        var controller = CreateController();

        var result = controller.GetDesigner(id) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(vm, result.Value);
    }

    [Fact]
    public void SetAllEditable_NotOnlyTable_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.SetAllEditable(Guid.NewGuid(), "t", true, TableSchemaQueryType.All);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void SetAllEditable_OnlyTable_ReturnsUpdatedFields()
    {
        var controller = CreateController();
        var formId = Guid.NewGuid();
        var fields = new FormFieldListViewModel();
        _designerMock.Setup(s => s.GetFieldsByTableName("t", TableSchemaQueryType.OnlyTable)).Returns(fields);

        var result = controller.SetAllEditable(formId, "t", true, TableSchemaQueryType.OnlyTable) as OkObjectResult;

        _designerMock.Verify(s => s.SetAllEditable(formId, "t", true), Times.Once);
        Assert.NotNull(result);
        var model = Assert.IsType<FormFieldListViewModel>(result.Value);
        Assert.Same(fields, model);
        Assert.Equal(formId, model.ID);
        Assert.Equal(TableSchemaQueryType.OnlyTable, model.SchemaQueryType);
    }

    [Fact]
    public void SetAllRequired_NotOnlyTable_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = controller.SetAllRequired(Guid.NewGuid(), "t", true, TableSchemaQueryType.All);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void SetAllRequired_OnlyTable_ReturnsUpdatedFields()
    {
        var controller = CreateController();
        var formId = Guid.NewGuid();
        var fields = new FormFieldListViewModel();
        _designerMock.Setup(s => s.GetFieldsByTableName("t", TableSchemaQueryType.OnlyTable)).Returns(fields);

        var result = controller.SetAllRequired(formId, "t", true, TableSchemaQueryType.OnlyTable) as OkObjectResult;

        _designerMock.Verify(s => s.SetAllRequired(formId, "t", true), Times.Once);
        Assert.NotNull(result);
        var model = Assert.IsType<FormFieldListViewModel>(result.Value);
        Assert.Same(fields, model);
        Assert.Equal(formId, model.ID);
        Assert.Equal(TableSchemaQueryType.OnlyTable, model.SchemaQueryType);
    }

    [Fact]
    public void SaveFormHeader_MissingNames_ReturnsBadRequest()
    {
        var controller = CreateController();
        var vm = new FormHeaderViewModel();

        var result = controller.SaveFormHeader(vm);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void SaveFormHeader_Duplicate_ReturnsConflict()
    {
        var controller = CreateController();
        var vm = new FormHeaderViewModel { TABLE_NAME = "T", VIEW_TABLE_NAME = "V" };
        _designerMock.Setup(s => s.CheckFormMasterExists("T", "V", vm.ID)).Returns(true);

        var result = controller.SaveFormHeader(vm);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void SaveFormHeader_Valid_ReturnsId()
    {
        var controller = CreateController();
        var vm  = new FormHeaderViewModel { TABLE_NAME = "T", VIEW_TABLE_NAME = "V", FORM_NAME = "F" };
        var id  = Guid.NewGuid();
        _designerMock.Setup(s => s.CheckFormMasterExists("T", "V", vm.ID)).Returns(false);
        _designerMock.Setup(s => s.SaveFormHeader(It.IsAny<FORM_FIELD_Master>())).Returns(id);

        var result = controller.SaveFormHeader(vm) as OkObjectResult;

        Assert.NotNull(result);
        var value = result.Value?.GetType().GetProperty("id")?.GetValue(result.Value);
        Assert.Equal(id, value);
    }
}

