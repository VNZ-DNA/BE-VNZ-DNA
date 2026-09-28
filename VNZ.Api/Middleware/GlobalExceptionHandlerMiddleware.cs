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
            if (IsPublicContactCreateRequest(context))
            {
                _logger.LogError(
                    "Public contact create failed. TraceId: {TraceId}, ExceptionType: {ExceptionType}",
                    context.TraceIdentifier,
                    exception.GetType().Name);
            }
            else
            {
                _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            }

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
            PartnerException partnerStatusException => GetPartnerStatusCode(partnerStatusException.Code),
            TeamMemberException teamMemberStatusException => GetTeamMemberStatusCode(teamMemberStatusException.Code),
            ContactException contactStatusException => GetContactStatusCode(contactStatusException.Code),
            JobApplicationException jobApplicationStatusException => GetJobApplicationStatusCode(jobApplicationStatusException.Code),
            MediaException mediaStatusException => GetMediaStatusCode(mediaStatusException.Code),
            DbException when IsCreateNewsRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsUpdateNewsRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsNewsDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsNewsListRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsPublicNewsListRequest(context) => HttpStatusCode.InternalServerError,
            Exception when IsPublicNewsListRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsPublicNewsDetailRequest(context) => HttpStatusCode.InternalServerError,
            Exception when IsPublicNewsDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsPublicProductListRequest(context) => HttpStatusCode.InternalServerError,
            Exception when IsPublicProductListRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsPublicPartnerListRequest(context) => HttpStatusCode.InternalServerError,
            Exception when IsPublicPartnerListRequest(context) => HttpStatusCode.InternalServerError,
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
                when IsPublicContactCreateRequest(context) => HttpStatusCode.RequestEntityTooLarge,
            DbUpdateException when IsPublicContactCreateRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsPublicContactCreateRequest(context) => HttpStatusCode.InternalServerError,
            Exception when IsPublicContactCreateRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsContactListRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsContactDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbUpdateException when IsContactDetailRequest(context) => HttpStatusCode.InternalServerError,
            DbException when IsContactReplyRequest(context) => HttpStatusCode.InternalServerError,
            DbUpdateException when IsContactReplyRequest(context) => HttpStatusCode.InternalServerError,
            HttpRequestException when IsContactReplyRequest(context) => HttpStatusCode.InternalServerError,
            TaskCanceledException when IsContactReplyRequest(context) => HttpStatusCode.InternalServerError,
            InvalidOperationException when IsContactReplyRequest(context) => HttpStatusCode.InternalServerError,
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
            PartnerException partnerPayloadException => (partnerPayloadException.Code, partnerPayloadException.Message),
            TeamMemberException teamMemberPayloadException => (teamMemberPayloadException.Code, teamMemberPayloadException.Message),
            ContactException contactPayloadException => (contactPayloadException.Code, contactPayloadException.Message),
            JobApplicationException jobApplicationPayloadException => (jobApplicationPayloadException.Code, jobApplicationPayloadException.Message),
            MediaException mediaPayloadException => (mediaPayloadException.Code, mediaPayloadException.Message),
            DbException when IsCreateNewsRequest(context) =>
                ("NEWS_ARTICLE_CREATE_FAILED", "Không thể tạo bài viết."),
            DbException when IsUpdateNewsRequest(context) =>
                ("NEWS_ARTICLE_UPDATE_FAILED", "Không thể cập nhật bài viết."),
            DbException when IsNewsDetailRequest(context) =>
                ("NEWS_DETAIL_READ_FAILED", "Không thể đọc chi tiết bài viết."),
            DbException when IsNewsListRequest(context) =>
                ("NEWS_LIST_READ_FAILED", "Không thể đọc danh sách bài viết."),
            DbException when IsPublicNewsListRequest(context) =>
                ("NEWS_PUBLIC_LIST_FAILED", "Không thể đọc danh sách tin tức."),
            Exception when IsPublicNewsListRequest(context) =>
                ("NEWS_PUBLIC_LIST_FAILED", "Không thể đọc danh sách tin tức."),
            DbException when IsPublicNewsDetailRequest(context) =>
                ("NEWS_PUBLIC_ARTICLE_FAILED", "Không thể đọc chi tiết tin tức."),
            Exception when IsPublicNewsDetailRequest(context) =>
                ("NEWS_PUBLIC_ARTICLE_FAILED", "Không thể đọc chi tiết tin tức."),
            DbException when IsPublicProductListRequest(context) =>
                ("PRODUCT_PUBLIC_LIST_FAILED", "Không thể lấy danh sách Product công khai."),
            Exception when IsPublicProductListRequest(context) =>
                ("PRODUCT_PUBLIC_LIST_FAILED", "Không thể lấy danh sách Product công khai."),
            DbException when IsPublicPartnerListRequest(context) =>
                ("PARTNER_PUBLIC_LIST_FAILED", "Không thể lấy danh sách Partner công khai."),
            Exception when IsPublicPartnerListRequest(context) =>
                ("PARTNER_PUBLIC_LIST_FAILED", "Không thể lấy danh sách Partner công khai."),
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
                when IsPublicContactCreateRequest(context) =>
                ("CONTACT_REQUEST_TOO_LARGE", "Dung lượng yêu cầu vượt giới hạn của hệ thống."),
            DbUpdateException when IsPublicContactCreateRequest(context) =>
                ("CONTACT_CREATE_FAILED", "Không thể tiếp nhận yêu cầu liên hệ lúc này. Vui lòng thử lại sau."),
            DbException when IsPublicContactCreateRequest(context) =>
                ("CONTACT_CREATE_FAILED", "Không thể tiếp nhận yêu cầu liên hệ lúc này. Vui lòng thử lại sau."),
            Exception when IsPublicContactCreateRequest(context) =>
                ("CONTACT_CREATE_FAILED", "Không thể tiếp nhận yêu cầu liên hệ lúc này. Vui lòng thử lại sau."),
            DbException when IsContactListRequest(context) =>
                ("CONTACT_LIST_READ_FAILED", "Không thể đọc danh sách liên hệ."),
            DbException when IsContactDetailRequest(context) =>
                ("CONTACT_DETAIL_READ_FAILED", "Không thể đọc chi tiết liên hệ."),
            DbUpdateException when IsContactDetailRequest(context) =>
                ("CONTACT_DETAIL_READ_FAILED", "Không thể đọc chi tiết liên hệ."),
            DbException when IsContactReplyRequest(context) =>
                ("CONTACT_REPLY_READ_FAILED", "Không thể đọc yêu cầu liên hệ để gửi phản hồi."),
            DbUpdateException when IsContactReplyRequest(context) =>
                ("CONTACT_REPLY_UPDATE_FAILED", "Không thể cập nhật trạng thái liên hệ."),
            HttpRequestException when IsContactReplyRequest(context) =>
                ("CONTACT_EMAIL_SEND_FAILED", "Không thể gửi email phản hồi."),
            TaskCanceledException when IsContactReplyRequest(context) =>
                ("CONTACT_EMAIL_SEND_FAILED", "Không thể gửi email phản hồi."),
            InvalidOperationException when IsContactReplyRequest(context) =>
                ("CONTACT_EMAIL_SEND_FAILED", "Không thể gửi email phản hồi."),
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
                                : exception is PartnerException partnerException
                                    ? partnerException.Fields
                                : exception is TeamMemberException teamMemberException
                                    ? teamMemberException.Fields
                                    : exception is ContactException contactException
                                        ? contactException.Fields
                                        : exception is JobApplicationException jobApplicationException
                                            ? jobApplicationException.Fields
                                        : exception is MediaException mediaException
                                            ? mediaException.Fields
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
            "AUTH_INVALID_REFRESH_TOKEN" or
            "AUTH_UNAUTHENTICATED" => HttpStatusCode.Unauthorized,
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
            
            "PUBLIC_JOB_POST_NOT_AVAILABLE" => HttpStatusCode.NotFound,
            "JOB_POST_CREATE_FAILED" => HttpStatusCode.InternalServerError,
            "JOB_POST_UPDATE_FAILED" or
            "PUBLIC_JOB_POST_DETAIL_READ_FAILED" => HttpStatusCode.InternalServerError,
            "PUBLIC_JOB_POST_LIST_READ_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetNewsStatusCode(string code)
    {
        return code switch
        {
            "NEWS_QUERY_INVALID" or
            "NEWS_PUBLIC_LIST_VALIDATION_FAILED" or
            "NEWS_PUBLIC_ARTICLE_ID_INVALID" or
            "NEWS_ARTICLE_ID_INVALID" or
            "NEWS_VALIDATION_ERROR" or
            "NEWS_CONTENT_TOO_SHORT" or
            "NEWS_STATUS_INVALID" or
            "NEWS_CATEGORY_REQUIRED" or
            "NEWS_CATEGORY_INVALID" => HttpStatusCode.BadRequest,
            "NEWS_ARTICLE_NOT_FOUND" or
            "NEWS_PUBLIC_ARTICLE_NOT_FOUND" => HttpStatusCode.NotFound,
            "NEWS_ARTICLE_CLOSED" or
            "NEWS_STATUS_TRANSITION_INVALID" => HttpStatusCode.Conflict,
            "NEWS_LIST_READ_FAILED" or
            "NEWS_PUBLIC_LIST_FAILED" or
            "NEWS_PUBLIC_ARTICLE_FAILED" or
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
            "PRODUCT_PUBLISHED_CANNOT_EDIT" or
            "PRODUCT_IN_PROGRESS_CANNOT_PUBLISH" or
            "PRODUCT_DELETE_FORBIDDEN" => HttpStatusCode.Conflict,
            "PRODUCT_LIST_READ_FAILED" or
            "PRODUCT_CREATE_FAILED" or
            "PRODUCT_ORDER_UPDATE_FAILED" or
            "PRODUCT_OPERATION_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetPartnerStatusCode(string code)
    {
        return code switch
        {
            "PARTNER_LIST_VALIDATION_FAILED" or
            "PARTNER_VALIDATION_FAILED" or
            "PARTNER_ORDER_INVALID" => HttpStatusCode.BadRequest,
            "PARTNER_NOT_FOUND" => HttpStatusCode.NotFound,
            "PARTNER_PUBLISHED_CANNOT_EDIT" => HttpStatusCode.Conflict,
            "PARTNER_LIST_READ_FAILED" or
            "PARTNER_CREATE_FAILED" or
            "PARTNER_ORDER_UPDATE_FAILED" or
            "PARTNER_UPDATE_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetTeamMemberStatusCode(string code)
    {
        return code switch
        {
            "MEMBER_VALIDATION_ERROR" or
            "MEMBER_QUERY_INVALID" => HttpStatusCode.BadRequest,
            "MEMBER_ORDER_INVALID" => HttpStatusCode.BadRequest,
            "MEMBER_ORDER_CONFLICT" => HttpStatusCode.Conflict,
            "MEMBER_ORDER_UPDATE_FAILED" => HttpStatusCode.InternalServerError,
            "MEMBER_EMAIL_EXISTS" => HttpStatusCode.Conflict,
            "MEMBER_CREATE_FAILED" or
            "MEMBER_LIST_READ_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetContactStatusCode(string code)
    {
        return code switch
        {
            "CONTACT_QUERY_INVALID" or
            "CONTACT_REPLY_VALIDATION_ERROR" or
            "CONTACT_CREATE_VALIDATION_FAILED" => HttpStatusCode.BadRequest,
            "CONTACT_NOT_FOUND" => HttpStatusCode.NotFound,
            "CONTACT_ALREADY_CONTACTED" => HttpStatusCode.Conflict,
            "CONTACT_LIST_READ_FAILED" or
            "CONTACT_DETAIL_READ_FAILED" or
            "CONTACT_REPLY_READ_FAILED" or
            "CONTACT_EMAIL_SEND_FAILED" or
            "CONTACT_REPLY_UPDATE_FAILED" or
            "CONTACT_CREATE_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetJobApplicationStatusCode(string code)
    {
        return code switch
        {
            "JOB_APPLICATION_REJECTION_REQUEST_INVALID" => HttpStatusCode.BadRequest,
            "JOB_APPLICATION_NOT_FOUND" => HttpStatusCode.NotFound,
            "INVALID_APPLICATION_STATUS" => HttpStatusCode.Conflict,
            "JOB_APPLICATION_REJECTION_EMAIL_FAILED" or
            "JOB_APPLICATION_REJECTION_PERSIST_FAILED" => HttpStatusCode.InternalServerError,
            "JOB_APPLICATION_INTERVIEW_REQUEST_INVALID" or
            "JOB_APPLICATION_INTERVIEW_TIME_INVALID" => HttpStatusCode.BadRequest,
            "JOB_APPLICATION_INTERVIEW_BATCH_INVALID" => HttpStatusCode.Conflict,
            "JOB_APPLICATION_INTERVIEW_PERSIST_FAILED" => HttpStatusCode.InternalServerError,
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static HttpStatusCode GetMediaStatusCode(string code)
    {
        return code switch
        {
            "MEDIA_FILE_REQUIRED" or
            "MEDIA_FILE_TYPE_UNSUPPORTED" or
            "MEDIA_FILE_TOO_LARGE" or
            "MEDIA_PURPOSE_UNSUPPORTED" => HttpStatusCode.BadRequest,
            "MEDIA_CONFIGURATION_INVALID" or
            "MEDIA_UPLOAD_FAILED" => HttpStatusCode.InternalServerError,
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

    private static bool IsPublicNewsListRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/public/news", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicNewsDetailRequest(HttpContext context)
    {
        const string routePrefix = "/api/v1/public/news/";

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
        return !string.IsNullOrWhiteSpace(id) && !id.Contains('/');
    }

    private static bool IsPublicProductListRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/public/products", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicPartnerListRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/public/partners", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContactListRequest(HttpContext context)
    {
        return HttpMethods.IsGet(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/admin/contacts", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicContactCreateRequest(HttpContext context)
    {
        return HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.Equals("/api/v1/public/contacts", StringComparison.OrdinalIgnoreCase);
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

    private static bool IsContactReplyRequest(HttpContext context)
    {
        const string routePrefix = "/api/v1/admin/contacts/";
        const string routeSuffix = "/reply";

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;
        if (path is null
            || !path.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase)
            || !path.EndsWith(routeSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var id = path[routePrefix.Length..^routeSuffix.Length];
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
