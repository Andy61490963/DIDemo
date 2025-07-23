using Dapper;
using DynamicForm.Models;
using DynamicForm.Service.Interface;
using Microsoft.Data.SqlClient;

namespace DynamicForm.Service.Service;

public class FormBuilderService : IFormBuilderService
{
    private readonly SqlConnection _con;
    
    public FormBuilderService(SqlConnection connection)
    {
        _con = connection;
    }

    public List<FormMaster> GetAllForms()
    {
        return _con.Query<FormMaster>("SELECT * FROM FormMaster ORDER BY CreateTime DESC").ToList();
    }

    public FormMaster GetFormById(int id)
    {
        return _con.QueryFirstOrDefault<FormMaster>("SELECT * FROM FormMaster WHERE FormId = @id", new { id });
    }

    public void CreateForm(FormMaster form)
    {
        string sql = @"
            INSERT INTO FormMaster (FormName, Description, IsActive)
            VALUES (@FormName, @Description, @IsActive);";

        _con.Execute(sql, form);
    }

    public void UpdateForm(FormMaster form)
    {
        string sql = "UPDATE FormMaster SET FormName = @FormName, Description = @Description WHERE FormId = @FormId";
        _con.Execute(sql, form);
    }

    public List<FormField> GetFieldsByFormId(int formId)
    {
        return _con.Query<FormField>("SELECT * FROM FormField WHERE FormId = @formId ORDER BY FieldOrder", new { formId }).ToList();
    }

    public FormField GetFieldById(int fieldId)
    {
        return _con.QueryFirstOrDefault<FormField>("SELECT * FROM FormField WHERE FieldId = @fieldId", new { fieldId });
    }

    public void AddField(FormField field)
    {
        string sql = @"
            INSERT INTO FormField (FormId, FieldLabel, FieldKey, FieldType, IsRequired, DefaultValue, FieldOptions, FieldOrder, Placeholder, CssClass, ValidationRules)
            VALUES (@FormId, @FieldLabel, @FieldKey, @FieldType, @IsRequired, @DefaultValue, @FieldOptions, @FieldOrder, @Placeholder, @CssClass, @ValidationRules);";

        _con.Execute(sql, field);
    }

    public void UpdateField(FormField field)
    {
        string sql = @"
            UPDATE FormField SET
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

        _con.Execute(sql, field);
    }

    public void DeleteField(int fieldId)
    {
        _con.Execute("DELETE FROM FormField WHERE FieldId = @fieldId", new { fieldId });
    }

    public void SaveResult(FormResult result)
    {
        string sql = @"
            INSERT INTO FormResult (FormId, SubmitUser, ResultJson)
            VALUES (@FormId, @SubmitUser, @ResultJson);";

        _con.Execute(sql, result);
    }
}