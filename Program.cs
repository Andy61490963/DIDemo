using DynamicForm.Service.Interface;
using DynamicForm.Service.Service;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using DynamicForm.Service.Interface.TransactionInterface;
using DynamicForm.Service.Service.FormLogicService;
using DynamicForm.Service.Service.TransactionService;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOptions();

builder.Services.AddScoped<ITransactionService, TransactionService>();
// Service
builder.Services.AddScoped<IFormListService, FormListService>();
builder.Services.AddScoped<IFormDesignerService, FormDesignerService>();


builder.Services.AddScoped<IFormFieldMasterService, FormFieldMasterService>();
builder.Services.AddScoped<ISchemaService, SchemaService>();
builder.Services.AddScoped<IFormFieldConfigService, FormFieldConfigService>();
builder.Services.AddScoped<IDropdownService, DropdownService>();
builder.Services.AddScoped<IFormDataService, FormDataService>();
builder.Services.AddScoped<IFormService, FormService>();



builder.Services.AddScoped<SqlConnection, SqlConnection>(_ =>
{
    var conn = new SqlConnection();
    conn.ConnectionString = builder.Configuration.GetConnectionString("Connection");
    return conn;
});

// Cors 先設定AllowAll
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
var app = builder.Build();

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>
/// 為了讓 WebApplicationFactory 能抓到 top-level Program.cs 的啟動點所需
/// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0&pivots=xunit
/// </summary>
public partial class Program { }