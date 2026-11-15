using BoneAI.Application.DependencyInjection;
using BoneAI.Infrastructure.DependencyInjection;
using BoneAI.API.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            var message = errors.Count > 0
                ? string.Join("; ", errors)
                : "Invalid request payload.";

            var errorResponse = new BoneAI.API.Models.ErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = message,
                TraceId = context.HttpContext.TraceIdentifier,
                Details = null
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errorResponse);
        };
    });

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BoneAI Web API",
        Version = "v1",
        Description = "Backend API for BoneAI - Pediatric Wrist Fracture Detection System"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BoneAI Web API v1");
        options.RoutePrefix = "swagger";
    });

    // Automatically ensure development database schema and seeded data exist for relational databases
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BoneAI.Infrastructure.Persistence.BoneAIDbContext>();
    try
    {
        if (dbContext.Database.IsSqlServer())
        {
            dbContext.Database.EnsureCreated();

            const string schemaFixSql = @"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Analyses')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Analyses') AND name = 'IsDeleted')
                        ALTER TABLE Analyses ADD IsDeleted BIT NOT NULL DEFAULT 0;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Analyses') AND name = 'DeletedAt')
                        ALTER TABLE Analyses ADD DeletedAt DATETIME2 NULL;
                END
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AnalysisRuns')
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AnalysisRuns') AND name = 'ResultImagePath')
                        ALTER TABLE AnalysisRuns ADD ResultImagePath NVARCHAR(500) NULL;
                END";
            dbContext.Database.ExecuteSqlRaw(schemaFixSql);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not ensure database creation on startup: {Message}", ex.Message);
    }
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

