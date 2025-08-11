using System.Reflection;
using DynamicForm.Service.Interface;
using DynamicForm.Service.Service;
using DynamicForm.Models;
using DynamicForm.Service.Interface.FormLogicInterface;
using DynamicForm.Service.Interface.TransactionInterface;
using DynamicForm.Service.Service.FormLogicService;
using DynamicForm.Service.Service.TransactionService;
using DynamicForm.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Swagger 註冊
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dynamic Form API",
        Version = "v1",
        Description = "表單設計系統的 API 文件",
    });

    // 加入 XML 註解（讓 <summary> 顯示在 Swagger UI）
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddOptions();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddScoped<ITransactionService, TransactionService>();
// Service
builder.Services.AddScoped<IFormListService, FormListService>();
builder.Services.AddScoped<IFormDesignerService, FormDesignerService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

// 加入 Swagger 中介軟體（無論開發或正式環境都開啟）
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamic Form API v1");
    options.RoutePrefix = string.Empty;
});

app.MapControllers();
app.Run();

/// <summary>
/// 為了讓 WebApplicationFactory 能抓到 top-level Program.cs 的啟動點所需
/// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0&pivots=xunit
/// </summary>
public partial class Program { }
