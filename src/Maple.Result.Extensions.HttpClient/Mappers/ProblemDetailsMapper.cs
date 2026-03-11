using Maple.Result.Extensions.HttpClient.Extensions;
using Maple.Result.Extensions.HttpClient.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class ProblemDetailsMapper
{
    #region consts

    private const string DetailTemplatedPropertyName = "detailTemplated";
    private const string ErrorDetailsPropertyName = "errors";

    #endregion

    internal static void Map(ProblemDetailsInternal? problemDetails, HttpResponseHeaders httpHeaders, ErrorBuilder errorBuilder)
    {
        if (problemDetails is null)
            return;

        var typeUri = ErrorUriMapper.MapTypeUri(problemDetails.Type);
        var instanceUri = ErrorUriMapper.Map(problemDetails.Instance);

        var detailTemplatedValue = problemDetails.Extensions.GetValueOrNull(DetailTemplatedPropertyName);
        var detailTemplated = TemplatedMessageMapper.TryMap(detailTemplatedValue);

        var errorDetailsValue = problemDetails.Extensions.GetValueOrNull(ErrorDetailsPropertyName);
        var errorDetails = ErrorDetailsMapper.TryMap(errorDetailsValue);

        errorBuilder
            .WithTypeUri(typeUri)
            .WithTitle(problemDetails.Title)
            .WithDetail(problemDetails.Detail)
            .WithInstanceUri(instanceUri)
            .WithDetailTemplated(detailTemplated);

        if (errorDetails is { Count: > 0 })
        {
            foreach (var errorDetail in errorDetails)
                errorBuilder.WithErrorDetail(errorDetail);
        }
    }
}
