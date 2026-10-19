using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SupermarketAPI.Endpoints;
using SupermarketAPI.Models;
using SupermarketAPI.Services.Products;
using SupermarketAPI.Services.Users;
using SupermarketAPI.Services.Brands;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => 
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el siguiente formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<SupermarketDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SupermarketDbConnection")
        ?? throw new InvalidOperationException("Connection string 'SupermarketDbConnection' is not configured.");
    var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
    var password = builder.Configuration["MSSQL_SA_PASSWORD"];
    if (string.IsNullOrWhiteSpace(password))
    {
        password = connectionStringBuilder.Password;
    }

    if (string.IsNullOrWhiteSpace(password))
    {
        if (builder.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        throw new InvalidOperationException("Configure MSSQL_SA_PASSWORD before starting the API.");
    }

    connectionStringBuilder.Password = password;

    options.UseSqlServer(connectionStringBuilder.ConnectionString);
});

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddScoped<IProductServices, ProductServices>();
builder.Services.AddScoped<IUserServices, UserServices>();
builder.Services.AddScoped<IBrandServices, BrandServices>();

var jwtSettings = builder.Configuration.GetSection("JwtSetting");
var secretKey = jwtSettings.GetValue<string>("SecretKey");

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(
options =>
{
    //Esquema por defecto
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(
options => { 
    //Permite usar HTTP en lugar de HTTPS
    options.RequireHttpsMetadata = false;
    //Guardar el token en el contexto de autenticaci�n
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SupermarketDbContext>();
    if (dbContext.Database.IsRelational())
    {
        var masterConnectionString = new SqlConnectionStringBuilder(
            dbContext.Database.GetDbConnection().ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using (var connection = new SqlConnection(masterConnectionString.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "IF DB_ID(@databaseName) IS NULL BEGIN DECLARE @createDatabaseSql nvarchar(258); SET @createDatabaseSql = N'CREATE DATABASE ' + QUOTENAME(@databaseName); EXEC(@createDatabaseSql); END;";
            command.Parameters.AddWithValue(
                "@databaseName",
                new SqlConnectionStringBuilder(dbContext.Database.GetDbConnection().ConnectionString).InitialCatalog);
            await command.ExecuteNonQueryAsync();
        }

        await dbContext.Database.MigrateAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   
}
 app.UseSwagger();
 app.UseSwaggerUI();
//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints();

app.Run();

public partial class Program { }
