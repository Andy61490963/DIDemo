using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using System.Data;

namespace DynamicForm.Service.Service;

public class FormBuilderService : IFormBuilderService
{
    private readonly IDbConnectionFactory _factory;

    public FormBuilderService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public List<FormMaster> GetAllForms()
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.Query<FormMaster>("SELECT * FROM FormMaster ORDER BY CreateTime DESC").ToList();
    }

    public FormMaster? GetFormById(int id)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.QueryFirstOrDefault<FormMaster>("SELECT * FROM FormMaster WHERE FormId = @id", new { id });
    }

    public void CreateForm(FormMaster form)
    {
        const string sql = @"INSERT INTO FormMaster (FormName, Description, IsActive)
            VALUES (@FormName, @Description, @IsActive);";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(sql, form);
    }

    public void UpdateForm(FormMaster form)
    {
        const string sql = "UPDATE FormMaster SET FormName = @FormName, Description = @Description WHERE FormId = @FormId";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(sql, form);
    }

    public List<FormField> GetFieldsByFormId(int formId)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.Query<FormField>("SELECT * FROM FormField WHERE FormId = @formId ORDER BY FieldOrder", new { formId }).ToList();
    }

    public FormField? GetFieldById(int fieldId)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        return conn.QueryFirstOrDefault<FormField>("SELECT * FROM FormField WHERE FieldId = @fieldId", new { fieldId });
    }

    public void AddField(FormField field)
    {
        const string sql = @"INSERT INTO FormField (FormId, FieldLabel, FieldKey, FieldType, IsRequired, DefaultValue, FieldOptions, FieldOrder, Placeholder, CssClass, ValidationRules)
            VALUES (@FormId, @FieldLabel, @FieldKey, @FieldType, @IsRequired, @DefaultValue, @FieldOptions, @FieldOrder, @Placeholder, @CssClass, @ValidationRules);";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(sql, field);
    }

    public void UpdateField(FormField field)
    {
        const string sql = @"UPDATE FormField SET
                FieldLabel = @FieldLabel,
                FieldKey = @FieldKey,
                FieldType = @FieldType,
                IsRequired = @IsRequired,
                DefaultValue = @DefaultValue,
                FieldOptions = @FieldOptions,
                FieldOrder = @FieldOrder,
                Placeholder = @Placeholder,
                CssClass = @CssClass,
                ValidationRules = @ValidationRules
            WHERE FieldId = @FieldId";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(sql, field);
    }

    public void DeleteField(int fieldId)
    {
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute("DELETE FROM FormField WHERE FieldId = @fieldId", new { fieldId });
    }

    public void SaveResult(FormResult result)
    {
        const string sql = @"INSERT INTO FormResult (FormId, SubmitUser, ResultJson)
            VALUES (@FormId, @SubmitUser, @ResultJson);";
        using var conn = _factory.CreateConnection();
        conn.Open();
        conn.Execute(sql, result);
    }
}