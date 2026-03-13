// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Result.Extensions.HttpClient.Converters;
using Maple.Result.Extensions.HttpClient.Helpers;
using Maple.Result.Extensions.HttpClient.Mappers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Maple.Result.Extensions.HttpClient;

/// <summary>
///     Contains extension methods to map the <see cref="HttpResponseMessage"/> to the <see cref="Result"/> and <see cref="Result{T}"/> types.
/// </summary>
public static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new ObjectAsPrimitiveConverter(), new JsonStringEnumConverter() }
    };

    public static Task<Result> ToResultAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return Task.FromResult(Result.Success());

        return MapToErrorAsync<ProblemDetailsInternal>(response, (error, _, _, _, errorBuilder) => ProblemDetailsMapper.Map(error, errorBuilder))
            .ContinueWith(t => Result.FromError(t.Result));
    }

    public static Task<Result> ToResultAsync<TError>(this HttpResponseMessage response,
        Action<TError?, ErrorBuilder> mapAction)
    {
        if (response.IsSuccessStatusCode)
            return Task.FromResult(Result.Success());

        return MapToErrorAsync(response,
                (TError? error, HttpStatusCode _, HttpResponseHeaders _, string _, ErrorBuilder errorBuilder)
                    => mapAction(error, errorBuilder))
            .ContinueWith(t => Result.FromError(t.Result));
    }

    public static Task<Result> ToResultAsync<TError>(this HttpResponseMessage response,
        Action<TError?, HttpStatusCode, ErrorBuilder> mapAction)
    {
        if (response.IsSuccessStatusCode)
            return Task.FromResult(Result.Success());

        return MapToErrorAsync(response,
                (TError? error, HttpStatusCode statusCode, HttpResponseHeaders _, string _, ErrorBuilder errorBuilder)
                    => mapAction(error, statusCode, errorBuilder))
            .ContinueWith(t => Result.FromError(t.Result));
    }

    public static Task<Result> ToResultAsync<TError>(this HttpResponseMessage response,
        Action<TError?, HttpStatusCode, HttpResponseHeaders, ErrorBuilder> mapAction)
    {
        if (response.IsSuccessStatusCode)
            return Task.FromResult(Result.Success());

        return MapToErrorAsync(response,
                (TError? error, HttpStatusCode statusCode, HttpResponseHeaders headers, string _, ErrorBuilder errorBuilder)
                    => mapAction(error, statusCode, headers, errorBuilder))
            .ContinueWith(t => Result.FromError(t.Result));
    }

    public static Task<Result> ToResultAsync<TError>(this HttpResponseMessage response,
        Action<TError?, HttpStatusCode, HttpResponseHeaders, string, ErrorBuilder> mapAction)
    {
        if (response.IsSuccessStatusCode)
            return Task.FromResult(Result.Success());

        return MapToErrorAsync(response, mapAction)
            .ContinueWith(t => Result.FromError(t.Result));
    }

    public static Task<Result<T>> ToResultAsync<T>(this HttpResponseMessage response)
        where T : notnull
    {
        if (response.IsSuccessStatusCode)
            return MapSuccessResponseAsync<T>(response);

        return MapToErrorAsync<ProblemDetailsInternal>(response, (error, _, _, _, errorBuilder) => ProblemDetailsMapper.Map(error, errorBuilder))
            .ContinueWith(t => Result<T>.FromError(t.Result));
    }

    public static Task<Result<T>> ToResultAsync<T, TError>(this HttpResponseMessage response,
        Action<TError?, ErrorBuilder> mapAction)
        where T : notnull
    {
        if (response.IsSuccessStatusCode)
            return MapSuccessResponseAsync<T>(response);

        return MapToErrorAsync(response,
                (TError? error, HttpStatusCode _, HttpResponseHeaders _, string _, ErrorBuilder errorBuilder)
                    => mapAction(error, errorBuilder))
            .ContinueWith(t => Result<T>.FromError(t.Result));
    }

    public static Task<Result<T>> ToResultAsync<T, TError>(this HttpResponseMessage response,
        Action<TError?, HttpStatusCode, ErrorBuilder> mapAction)
        where T : notnull
    {
        if (response.IsSuccessStatusCode)
            return MapSuccessResponseAsync<T>(response);

        return MapToErrorAsync(response,
                (TError? error, HttpStatusCode statusCode, HttpResponseHeaders _, string _, ErrorBuilder errorBuilder)
                    => mapAction(error, statusCode, errorBuilder))
            .ContinueWith(t => Result<T>.FromError(t.Result));
    }

    public static Task<Result<T>> ToResultAsync<T, TError>(this HttpResponseMessage response,
        Action<TError?, HttpStatusCode, HttpResponseHeaders, ErrorBuilder> mapAction)
        where T : notnull
    {
        if (response.IsSuccessStatusCode)
            return MapSuccessResponseAsync<T>(response);

        return MapToErrorAsync(response,
                (TError? error, HttpStatusCode statusCode, HttpResponseHeaders headers, string _, ErrorBuilder errorBuilder)
                    => mapAction(error, statusCode, headers, errorBuilder))
            .ContinueWith(t => Result<T>.FromError(t.Result));
    }

    public static Task<Result<T>> ToResultAsync<T, TError>(this HttpResponseMessage response,
        Action<TError?, HttpStatusCode, HttpResponseHeaders, string, ErrorBuilder> mapAction)
        where T : notnull
    {
        if (response.IsSuccessStatusCode)
            return MapSuccessResponseAsync<T>(response);

        return MapToErrorAsync(response, mapAction)
            .ContinueWith(t => Result<T>.FromError(t.Result));
    }

    private static async Task<Result<T>> MapSuccessResponseAsync<T>(HttpResponseMessage response)
        where T : notnull
    {
        var content = await response.Content.ReadAsStringAsync();

        // Special case for string content, since the JSON deserializer would expect a JSON string (with quotes) and fail if the content is a plain string without quotes.
        if (content is T tValue && !(content.StartsWith('"') && content.EndsWith('"')))
            return tValue;

        // Special case for enum types, since the JSON deserializer would expect a JSON string (with quotes) and fail if the content is a plain string without quotes.
        if (typeof(T).IsEnum
            && !(content.StartsWith('"') && content.EndsWith('"'))
            && Enum.TryParse(typeof(T), content, true, out var enumValue))
        {
            return (T)enumValue;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(content, JsonSerializerOptions);
            if (value is not null)
                return value;
        }
        catch (Exception ex)
        {
            return Error.Failure(
                ErrorUri.Tag("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid"),
                "Failed to deserialize the HTTP response content.",
                $"Error occured when deserializing the HTTP response content to the {typeof(T).FullName}: “{ex.Message}”.",
                null,
                "maple.result.httpClient.successResponse.json.deserialization.exception",
                ("exceptionType", ex.GetType().FullName ?? ex.GetType().Name),
                ("baseExceptionType",
                    ex.GetBaseException().GetType().FullName ?? ex.GetBaseException().GetType().Name),
                ("exceptionMessage", ex.Message),
                ("baseExceptionMessage", ex.GetBaseException().Message));
        }

        return Error.Failure(
            ErrorUri.Tag("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid"),
            "Failed to deserialize the HTTP response content.",
            $"Failed to deserialize the HTTP response content to the {typeof(T).FullName}.",
            null,
            "maple.result.httpClient.successResponse.json.deserialization.error");
    }

    private static async Task<Error> MapToErrorAsync<TError>(HttpResponseMessage response, Action<TError?, HttpStatusCode, HttpResponseHeaders, string, ErrorBuilder> mapAction)
    {
        try
        {
            var errorContent = await response.Content.ReadAsStringAsync();

            TError? mappedError = default;
            string? errorTitle = null;

            if (!string.IsNullOrWhiteSpace(errorContent))
            {
                if (JsonHelper.IsValidJson(errorContent, out var jsonDoc))
                    mappedError = JsonHelper.TryDeserialize<TError>(jsonDoc);
                else
                    errorTitle = errorContent;
            }

            var errorCategory = ErrorCategoryMapper.Map(response.StatusCode);
            errorTitle ??= ErrorTitleMapper.Map(response.ReasonPhrase, response.StatusCode);

            var errorBuilder = new ErrorBuilder(errorCategory, errorTitle);
            mapAction(mappedError, response.StatusCode, response.Headers, errorContent, errorBuilder);
            var error = errorBuilder.Build();

            return error;
        }
        catch (Exception ex)
        {
            return Error.Failure(
                ErrorUri.Tag("tag:mapledev.engineer,2026:result.httpClient.errorResponse.json.invalid"),
                "Failed to deserialize the HTTP error response content.",
                $"Error occured when deserializing the HTTP error response content to the {typeof(TError).FullName}: “{ex.Message}”.",
                null,
                "maple.result.httpClient.errorResponse.json.deserialization.exception",
                ("exceptionType", ex.GetType().FullName ?? ex.GetType().Name),
                ("baseExceptionType", ex.GetBaseException().GetType().FullName ?? ex.GetBaseException().GetType().Name),
                ("exceptionMessage", ex.Message),
                ("baseExceptionMessage", ex.GetBaseException().Message));
        }
    }
}
