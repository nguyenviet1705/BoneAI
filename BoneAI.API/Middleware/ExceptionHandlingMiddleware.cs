using System.Net;
using System.Text.Json;
using BoneAI.API.Models;
using BoneAI.Domain.Exceptions;

namespace BoneAI.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int statusCode;
        string message;

        if (exception is AppException appException)
        {
            statusCode = appException.StatusCode;
            message = appException.Message;

            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Application exception ({StatusCode}): {Message}", statusCode, message);
            }
            else
            {
                _logger.LogWarning("Handled client application exception ({StatusCode}): {Message}", statusCode, message);
            }
        }
        else
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            message = "An unexpected internal server error occurred.";
            _logger.LogError(exception, "Unhandled unexpected exception: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            TraceId = context.TraceIdentifier,
            Details = _env.IsDevelopment() ? exception.ToString() : null
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}
