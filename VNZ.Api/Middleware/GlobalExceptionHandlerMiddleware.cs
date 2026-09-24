using System.Net;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
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
            NewsException newsStatusException => GetNewsStatusCode(newsStatusException.Code),
            ProductException productStatusException => GetProductStatusCode(productStatusException.Code),
            TeamMemberException teamMemberStatusException => GetTeamMemberStatusCode(teamMemberStatusException.Code),
            ContactException contactStatusException => GetContactStatusCode(contactStatusException.Code),
            DbException when IsCreateNewsRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsUpdateNewsRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsNewsDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsNewsListRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsContactListRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsContactDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbUpdateException when IsContactDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbUpdateException when IsCreateMemberRequest(context) || IsCreateJobPostRequest(context) =>
                HttpStatusCode.InternalServerError,
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
            NewsException newsPayloadException => (newsPayloadException.Code, newsPayloadException.Message),
            ProductException productPayloadException => (productPayloadException.Code, productPayloadException.Message),
            TeamMemberException teamMemberPayloadException => (teamMemberPayloadException.Code, teamMemberPayloadException.Message),
            ContactException contactPayloadException => (contactPayloadException.Code, contactPayloadException.Message),
            DbException when IsCreateNewsRequest(context) =>
                ("NEWS_ARTICLE_CREATE_FAILED", "Không thể tạo bài viết."),
            DbException when IsUpdateNewsRequest(context) =>
                ("NEWS_ARTICLE_UPDATE_FAILED", "Không thể cập nhật bài viết."),
            DbException when IsNewsDetailRequest(context) =>
                ("NEWS_DETAIL_READ_FAILED", "Không thể đọc chi tiết bài viết."),
            DbException when IsNewsListRequest(context) =>
                ("NEWS_LIST_READ_FAILED", "Không thể đọc danh sách bài viết."),
            DbException when IsContactListRequest(context) =>
                ("CONTACT_LIST_READ_FAILED", "Không thể đọc danh sách liên hệ."),
            DbException when IsContactDetailRequest(context) =>
                ("CONTACT_DETAIL_READ_FAILED", "Không thể đọc chi tiết liên hệ."),
            DbUpdateException when IsContactDetailRequest(context) =>
                ("CONTACT_DETAIL_READ_FAILED", "Không thể đọc chi tiết liên hệ."),
            DbUpdateException when IsCreateMemberRequest(context) =>
                ("MEMBER_CREATE_FAILED", "Không thể tạo thành viên."),
            DbUpdateException when IsCreateJobPostRequest(context) =>
                ("JOB_POST_CREATE_FAILED", "Không thể tạo tin tuyển dụng."),
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
                            : exception is NewsException newsException
                                ? newsException.Fields
                                : exception is ProductException productException
                                    ? productException.Fields
                                : exception is TeamMemberException teamMemberException
                                    ? teamMemberException.Fields
                                    : exception is ContactException contactException
                                        ? contactException.Fields
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
            "JOB_POST_INVALID_ACTION" or
            "JOB_POST_INVALID_REQUEST" or
            "JOB_POST_INVALID_STATUS" or
            "JOB_POST_VALIDATION_FAILED" or
            "JOB_POST_VALIDATION_ERROR" or
            "JOB_POST_INVALID_EXPIRY" or
            "JOB_POST_INVALID_PAGINATION" or
            "JOB_POST_INVALID_SEARCH" or
            "JOB_POST_INVALID_STATUS_FILTER" => HttpStatusCode.BadRequest,
            "JOB_POST_NOT_FOUND" => HttpStatusCode.NotFound,
            "DEPARTMENT_NOT_FOUND" => HttpStatusCode.NotFound,
            "JOB_POST_CLOSED" or
            "JOB_POST_EXPIRED" => HttpStatusCode.Conflict,
            "JOB_POST_CREATE_FAILED" => HttpStatusCode.InternalServerError,
            "JOB_POST_UPDATE_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetNewsStatusCode(string code)
    {
        return code switch
        {
            "NEWS_QUERY_INVALID" or
            "NEWS_ARTICLE_ID_INVALID" or
            "NEWS_VALIDATION_ERROR" or
            "NEWS_CONTENT_TOO_SHORT" or
            "NEWS_STATUS_INVALID" or
            "NEWS_CATEGORY_REQUIRED" or
            "NEWS_CATEGORY_INVALID" => HttpStatusCode.BadRequest,
            "NEWS_ARTICLE_NOT_FOUND" => HttpStatusCode.NotFound,
            "NEWS_ARTICLE_CLOSED" or
            "NEWS_STATUS_TRANSITION_INVALID" => HttpStatusCode.Conflict,
            "NEWS_LIST_READ_FAILED" or
            "NEWS_DETAIL_READ_FAILED" or
            "NEWS_ARTICLE_CREATE_FAILED" or
            "NEWS_ARTICLE_UPDATE_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetProductStatusCode(string code)
    {
        return code switch
        {
            "PRODUCT_LIST_QUERY_INVALID" or
            "PRODUCT_ORDER_INVALID" or
            "PRODUCT_VALIDATION_FAILED" or
            "PRODUCT_VALIDATION_ERROR" or
            "PRODUCT_CONTENT_INVALID" => HttpStatusCode.BadRequest,
            "PRODUCT_NOT_FOUND" => HttpStatusCode.NotFound,
            "PRODUCT_ORDER_CONFLICT" or
            "PRODUCT_IN_PROGRESS_CANNOT_PUBLISH" or
            "PRODUCT_DELETE_FORBIDDEN" => HttpStatusCode.Conflict,
            "PRODUCT_LIST_READ_FAILED" or
            "PRODUCT_CREATE_FAILED" or
            "PRODUCT_ORDER_UPDATE_FAILED" or
            "PRODUCT_OPERATION_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetTeamMemberStatusCode(string code)
    {
        return code switch
        {
            "MEMBER_VALIDATION_ERROR" => HttpStatusCode.BadRequest,
            "MEMBER_ORDER_INVALID" => HttpStatusCode.BadRequest,
            "MEMBER_ORDER_CONFLICT" => HttpStatusCode.Conflict,
            "MEMBER_ORDER_UPDATE_FAILED" => HttpStatusCode.InternalServerError,
            "MEMBER_EMAIL_EXISTS" => HttpStatusCode.Conflict,
            "MEMBER_CREATE_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetContactStatusCode(string code)
    {
        return code switch
        {
            "CONTACT_QUERY_INVALID" or
            "CONTACT_ID_INVALID" => HttpStatusCode.BadRequest,
            "CONTACT_NOT_FOUND" => HttpStatusCode.NotFound,
            "CONTACT_LIST_READ_FAILED" => HttpStatusCode.InternalServerError,
            "CONTACT_DETAIL_READ_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static bool IsCreateMemberRequest(HttpContext context)
    {
        return HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/admin/team-members", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNewsListRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/admin/news", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContactListRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/admin/contacts", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContactDetailRequest(HttpContext context)
    {
        const string routePrefix = "/api/v1/admin/contacts/";

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;

        if (path is null || !path.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var id = path[routePrefix.Length..];
        return Guid.TryParse(id, out _);
    }

    private static bool IsNewsDetailRequest(HttpContext context)
    {
        const string routePrefix = "/api/v1/admin/news/";

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;

        if (path is null || !path.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var id = path[routePrefix.Length..];
        return Guid.TryParse(id, out _);
    }

    private static bool IsCreateNewsRequest(HttpContext context)
    {
        return HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/admin/news", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUpdateNewsRequest(HttpContext context)
    {
        const string routePrefix = "/api/v1/admin/news/";

        if (!HttpMethods.IsPut(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;

        if (path is null || !path.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var id = path[routePrefix.Length..];
        return Guid.TryParse(id, out _);
    }

    private static bool IsCreateJobPostRequest(HttpContext context)
    {
        return HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/admin/job-posts", StringComparison.OrdinalIgnoreCase);
    }
}
