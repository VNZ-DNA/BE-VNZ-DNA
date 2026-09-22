using System.Net;
using VNZ.Service.Exceptions;
using VNZ.Service.Models;

namespace VNZ.Api.Middleware;

public class GlobalExceptionHandlerMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,
            ConflictException => HttpStatusCode.Conflict,
            UnauthorizedException => HttpStatusCode.Unauthorized,
            ArgumentException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var (errorCode, message) = exception switch
        {
            NotFoundException =>
                ("RESOURCE_NOT_FOUND", exception.Message),
            ConflictException =>
                ("RESOURCE_CONFLICT", exception.Message),
            UnauthorizedException =>
                ("AUTH_UNAUTHENTICATED", exception.Message),
            ArgumentException =>
                ("RESOURCE_VALIDATION_FAILED", exception.Message),
            _ =>
                ("RESOURCE_OPERATION_FAILED", "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.")
        };

        var response = ResponseBuilder.ErrorResponse(
            errors: new
            {
                code = errorCode,
                fields = Array.Empty<string>()
            },
            message: message,
            traceId: context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(response);
    }
}
