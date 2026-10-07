using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VNZ.Service.Models;

namespace VNZ.Api.Filters;

/// <summary>
/// Final MVC safety net: content-type and validation results must use the
/// application's ApiResponse envelope instead of leaking framework-specific
/// ProblemDetails/empty 415 responses.
/// </summary>
public sealed class ApiResponseErrorResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is UnsupportedMediaTypeResult)
        {
            var isContact = context.HttpContext.Request.Path.Equals(
                "/api/v1/public/contacts",
                StringComparison.OrdinalIgnoreCase);

            context.Result = CreateErrorResult(
                StatusCodes.Status415UnsupportedMediaType,
                isContact ? "CONTACT_CONTENT_TYPE_UNSUPPORTED" : "BINDING_INVALID",
                context.HttpContext.TraceIdentifier,
                Array.Empty<string>());

            return;
        }

        if (context.Result is ObjectResult
            {
                StatusCode: StatusCodes.Status400BadRequest,
                Value: ValidationProblemDetails validationProblemDetails
            })
        {
            var fields = validationProblemDetails.Errors.Keys
                .Select(NormalizeField)
                .Where(field => !string.IsNullOrWhiteSpace(field))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            context.Result = CreateErrorResult(
                StatusCodes.Status400BadRequest,
                "BINDING_INVALID",
                context.HttpContext.TraceIdentifier,
                fields);
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static ObjectResult CreateErrorResult(
        int statusCode,
        string code,
        string traceId,
        IReadOnlyList<string> fields)
    {
        return new ObjectResult(ResponseBuilder.ErrorResponse(
            errors: new ApiError
            {
                Code = code,
                Fields = fields
            },
            message: "Request binding is invalid.",
            traceId: traceId))
        {
            StatusCode = statusCode
        };
    }

    private static string NormalizeField(string field)
    {
        var normalized = field.Trim();
        return normalized.StartsWith("$.", StringComparison.Ordinal)
            ? normalized[2..]
            : normalized;
    }
}
