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
            AuthException authStatusException => GetAuthStatusCode(authStatusException.Code),
            JobPostException jobPostStatusException => GetJobPostStatusCode(jobPostStatusException.Code),
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
            AuthException authPayloadException => (authPayloadException.Code, authPayloadException.Message),
            JobPostException jobPostPayloadException => (jobPostPayloadException.Code, jobPostPayloadException.Message),
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
                fields = exception is AuthException authException
                    ? authException.Fields
                    : exception is JobPostException jobPostException
                        ? jobPostException.Fields
                    : Array.Empty<string>()
            },
            message: message,
            traceId: context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(response);
    }

    private static HttpStatusCode GetAuthStatusCode(string code)
    {
        return code switch
        {
            "AUTH_REQUIRED_FIELDS" or
            "AUTH_REQUIRED_REFRESH_TOKEN" or
            "AUTH_INVALID_EMAIL" or
            "AUTH_INVALID_REQUEST" => HttpStatusCode.BadRequest,
            "AUTH_INVALID_CREDENTIALS" or
            "AUTH_ACCOUNT_INACTIVE" or
            "AUTH_INVALID_REFRESH_TOKEN" => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetJobPostStatusCode(string code)
    {
        return code switch
        {
            "JOB_POST_INVALID_PAGINATION" or
            "JOB_POST_INVALID_SEARCH" or
            "JOB_POST_INVALID_STATUS_FILTER" => HttpStatusCode.BadRequest,
            "JOB_POST_NOT_FOUND" => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
