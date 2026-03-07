using System.Collections.Generic;
using System.Linq;
using Maple.Result.Extensions.HttpClient.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Maple.Result.Extensions.HttpClient.Extensions;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class ErrorMapper
{
    #region consts

    private const string DetailTemplatedPropertyName = "detailTemplated";
    private const string ErrorDetailsPropertyName = "errors";

    #endregion

    internal static Error? TryMap(string? content, HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var problemDetails = JsonHelper.TryDeserialize<ProblemDetailsInternal>(content);
        if (problemDetails is null)
            return null;

        var title = problemDetails.Title;
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var typeUri = ErrorUriMapper.MapTypeUri(problemDetails.Type);
        var instanceUri = ErrorUriMapper.Map(problemDetails.Instance);
        
        var detailTemplatedValue = problemDetails.Extensions.GetValueOrNull(DetailTemplatedPropertyName);
        var detailTemplated = TemplatedMessageMapper.TryMap(detailTemplatedValue);

        var errorDetailsValue = problemDetails.Extensions.GetValueOrNull(ErrorDetailsPropertyName);
        var errorDetails = ErrorDetailsMapper.TryMap(errorDetailsValue);

        var errorCategory = ErrorCategoryMapper.Map(statusCode);

        var error = errorCategory switch
        {
            ErrorCategory.Validation => Error.Validation(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Unauthenticated => Error.Unauthenticated(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Unauthorized => Error.Unauthorized(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.NotFound => Error.NotFound(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Timeout => Error.Timeout(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Conflict => Error.Conflict(typeUri, title,problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Failure => Error.Failure(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Critical => Error.Critical(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.NotImplemented => Error.NotImplemented(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            ErrorCategory.Unavailable => Error.Unavailable(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params),
            _ => Error.Critical(typeUri, title, problemDetails.Detail, instanceUri, detailTemplated?.TemplateId, detailTemplated?.Params)
        };

        AddErrorDetails(error, errorDetails);

        return error;
    }

    private static void AddErrorDetails(Error error, IReadOnlyList<ErrorDetail>? errorDetails)
    {
        if (errorDetails is not {Count:>0})
            return;

        foreach (var errorDetail in errorDetails)
        {
            var namedValues = errorDetail.DetailTemplated?.Params
                                  ?.Select(kv => (kv.Key, kv.Value))
                                  .ToArray()
                              ?? [];

            error.AddDetail(errorDetail.PropertyPointer, errorDetail.Detail, errorDetail.DetailTemplated?.TemplateId, namedValues);
        }
    }
}
