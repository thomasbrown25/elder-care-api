global using elder_care_api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.Filters;
using elder_care_api.Data;
using elder_care_api.Logger;
using elder_care_api.DbLogger;
using Newtonsoft.Json;
using elder_care_api.Services.HealthService;

var builder = WebApplication.CreateBuilder(args);
var configBuilder = new ConfigurationBuilder();
var services = builder.Services;
var allowMyOrigins = "AllowMyOrigins";
var azureConnectionString = "";

builder.Logging.ClearProviders();

azureConnectionString = Environment.GetEnvironmentVariable("AzureAppConfiguration");

azureConnectionString ??= builder.Configuration.GetConnectionString("AzureAppConfiguration");

configBuilder.AddAzureAppConfiguration(azureConnectionString);

var configuration = configBuilder.Build();

// Services
services.AddHttpClient();

services.AddSingleton<IConfiguration>(configuration);

// Add logging
builder.Logging.AddConsole();
builder.Logging.AddEventSourceLogger();

// Add services to the container.
services.AddDbContext<DataContext>(
    options =>
    {
        options.UseSqlServer(configuration["Database.ConnectionString"]);
    },
        ServiceLifetime.Transient
);

services.AddControllers();

// Turn off claim mapping for Microsoft middleware
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition(
        "oauth2",
        new OpenApiSecurityScheme
        {
            Description =
                "Standard Authorization header using the Bearer scheme, e.g. \"bearer {token} \"",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey
        }
    );
    c.OperationFilter<SecurityRequirementsOperationFilter>();
});
services.AddSwaggerGenNewtonsoftSupport();
services.AddAutoMapper(typeof(Program).Assembly);
services.AddScoped<IUserService, UserService>();

services.AddTransient<ILogging, Logging>();




// Authentication
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(configuration["AppSettings.Key"])
            ),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });


services.AddHttpContextAccessor();
services.AddSingleton<IPrincipal>(
    provider => provider.GetService<IHttpContextAccessor>().HttpContext.User
);


services.AddCors(options =>
{
    options.AddPolicy(
        allowMyOrigins,
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:5000", "https://localhost:5000", "https://financing-app.azurewebsites.net", "https://money-clarity.azurewebsites.net", "https://finance-management.azurewebsites.net")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});

// Health check to database
services.AddHealthChecks()
.AddCheck<HealthService>("HealthCheck", failureStatus: HealthStatus.Degraded)
.AddDbContextCheck<DataContext>();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status500InternalServerError,
        [HealthStatus.Unhealthy] = StatusCodes.Status500InternalServerError
    },
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            HealthChecks = report.Entries.Select(x => new IndividualHealthCheckResponse
            {
                Component = x.Key,
                Status = x.Value.Status.ToString()
            }),
            HealthCheckDuration = report.TotalDuration
        };
        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    azureConnectionString = builder.Configuration.GetConnectionString("AzureAppConfiguration");
}
else
{

}

app.UseCors(allowMyOrigins);

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
