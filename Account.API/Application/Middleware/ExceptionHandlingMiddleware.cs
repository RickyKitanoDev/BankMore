using Account.API.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace Account.API.Application.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is BusinessException be)
        {
            // Map specific business error types to status codes
            var status = be.ErrorType == "USER_UNAUTHORIZED" ? HttpStatusCode.Unauthorized : HttpStatusCode.BadRequest;

            context.Response.StatusCode = (int)status;

            var payload = new {
                message = be.Message,
                type = be.ErrorType
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }

        // Log unhandled exceptions with more details
        Console.WriteLine($"Unhandled Exception: {exception.GetType().Name}");
        Console.WriteLine($"Message: {exception.Message}");
        Console.WriteLine($"StackTrace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            Console.WriteLine($"Inner Exception: {exception.InnerException.Message}");
        }

        // Unhandled exception
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var result = JsonSerializer.Serialize(new { 
            message = "An unexpected error occurred.", 
            detail = exception.Message,
            type = "INTERNAL_ERROR"
        });
        return context.Response.WriteAsync(result);
    }
}
