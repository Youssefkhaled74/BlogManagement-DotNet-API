using BlogManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
                DbUpdateException => (StatusCodes.Status409Conflict, "Database conflict"),
                InvalidOperationException => (StatusCodes.Status400BadRequest, "Invalid operation"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled request exception");

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var detail = exception is DbUpdateException
                ? "The operation conflicts with existing database data."
                : status == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            });
        }
    }
}
