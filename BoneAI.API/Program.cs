using BoneAI.Application.DependencyInjection;
using BoneAI.Infrastructure.DependencyInjection;
using BoneAI.API.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

