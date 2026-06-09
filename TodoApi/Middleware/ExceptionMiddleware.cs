using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Common;

namespace TodoApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        _logger.LogError(
            exception,
            "Unhandled exception occurred");

        var (statusCode, title) = exception switch
        {
            NotFoundException =>
                (StatusCodes.Status404NotFound, "Resource Not Found"),

            ValidationException =>
                (StatusCodes.Status400BadRequest, "Validation Failed"),

            _ =>
                (StatusCodes.Status500InternalServerError,
                    "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = statusCode == 500
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] =
            context.TraceIdentifier;

        context.Response.StatusCode = statusCode;

        await Results.Problem(
                title: problem.Title,
                detail: problem.Detail,
                statusCode: problem.Status,
                extensions: problem.Extensions)
            .ExecuteAsync(context);
    }
}