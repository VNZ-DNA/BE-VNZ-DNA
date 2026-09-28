using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VNZ.Service.Models;

namespace VNZ.Api.Filters;

public class PublicContactApiResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is UnsupportedMediaTypeResult)
        {
            context.Result = CreateErrorResult(
                StatusCodes.Status415UnsupportedMediaType,
                "CONTACT_CONTENT_TYPE_UNSUPPORTED",
                "Định dạng nội dung yêu cầu không được hỗ trợ.",
                Array.Empty<string>(),
                context.HttpContext.TraceIdentifier);

            return;
        }

        if (context.Result is ObjectResult
            {
                StatusCode: StatusCodes.Status400BadRequest,
                Value: ValidationProblemDetails validationProblemDetails
            })
        {
            var fields = validationProblemDetails.Errors.Keys
                .Select(GetFieldName)
                .Where(field => field is not null)
                .Cast<string>()
                .Distinct()
                .ToArray();

            context.Result = CreateErrorResult(
                StatusCodes.Status400BadRequest,
                "CONTACT_CREATE_VALIDATION_FAILED",
                "Thông tin liên hệ không hợp lệ.",
                fields,
                context.HttpContext.TraceIdentifier);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static ObjectResult CreateErrorResult(
        int statusCode,
        string errorCode,
        string message,
        string[] fields,
        string traceId)
    {
        return new ObjectResult(ResponseBuilder.ErrorResponse(
            errors: new
            {
                code = errorCode,
                fields
            },
            message: message,
            traceId: traceId))
        {
            StatusCode = statusCode
        };
    }

    private static string? GetFieldName(string modelStateKey)
    {
        var fieldName = modelStateKey.Trim();

        if (fieldName.StartsWith("$.", StringComparison.Ordinal))
        {
            fieldName = fieldName[2..];
        }

        var lastDotIndex = fieldName.LastIndexOf('.');
        if (lastDotIndex >= 0)
        {
            fieldName = fieldName[(lastDotIndex + 1)..];
        }

        if (string.Equals(fieldName, "inquiryTopic", StringComparison.OrdinalIgnoreCase))
        {
            return "inquiryTopic";
        }

        if (string.Equals(fieldName, "fullName", StringComparison.OrdinalIgnoreCase))
        {
            return "fullName";
        }

        if (string.Equals(fieldName, "email", StringComparison.OrdinalIgnoreCase))
        {
            return "email";
        }

        if (string.Equals(fieldName, "phone", StringComparison.OrdinalIgnoreCase))
        {
            return "phone";
        }

        if (string.Equals(fieldName, "companyName", StringComparison.OrdinalIgnoreCase))
        {
            return "companyName";
        }

        if (string.Equals(fieldName, "budgetRange", StringComparison.OrdinalIgnoreCase))
        {
            return "budgetRange";
        }

        if (string.Equals(fieldName, "expectedStart", StringComparison.OrdinalIgnoreCase))
        {
            return "expectedStart";
        }

        if (string.Equals(fieldName, "message", StringComparison.OrdinalIgnoreCase))
        {
            return "message";
        }

        if (string.Equals(fieldName, "source", StringComparison.OrdinalIgnoreCase))
        {
            return "source";
        }

        if (string.Equals(fieldName, "consentToDataProcessing", StringComparison.OrdinalIgnoreCase))
        {
            return "consentToDataProcessing";
        }

        return null;
    }
}
