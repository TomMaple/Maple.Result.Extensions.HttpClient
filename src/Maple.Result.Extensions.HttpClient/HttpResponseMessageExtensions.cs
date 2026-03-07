using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Maple.Result.Extensions.HttpClient.Converters;
using Maple.Result.Extensions.HttpClient.Mappers;

namespace Maple.Result.Extensions.HttpClient;

public static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,  // TODO: do we need that?
        Converters = { new ObjectAsPrimitiveConverter() }
    };

    public static Task<Result> ToResultAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return Task.FromResult(Result.Success());

        return MapToErrorAsync(response)
            .ContinueWith(t => Result.FromError(t.Result));
    }

    public static Task<Result<T>> ToResultAsync<T>(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return MapSuccessResponse<T>(response);

        return MapToErrorAsync(response)
            .ContinueWith(t => Result<T>.FromError(t.Result));
    }

    private static async Task<Result<T>> MapSuccessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var value = JsonSerializer.Deserialize<T>(content);
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

    private static async Task<Error> MapToErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var error = ErrorMapper.TryMap(errorContent, response.StatusCode);

            if (error is not null)
                return error;

            return Error.Failure(
                ErrorUri.Tag("tag:mapledev.engineer,2026:result.httpClient.errorResponse.json.invalid"),
                "Failed to deserialize the HTTP error response content.",
                "Error occured when deserializing the HTTP error response content.",
                null,
                "maple.result.httpClient.errorResponse.json.deserialization.error");
        }
        catch (Exception ex)
        {
            return Error.Failure(
                ErrorUri.Tag("tag:mapledev.engineer,2026:result.httpClient.errorResponse.json.invalid"),
                "Failed to deserialize the HTTP response content.",
                $"Error occured when deserializing the HTTP error response content: “{ex.Message}”.",
                null,
                "maple.result.httpClient.errorResponse.json.deserialization.exception",
                ("exceptionType", ex.GetType().FullName ?? ex.GetType().Name),
                ("baseExceptionType", ex.GetBaseException().GetType().FullName ?? ex.GetBaseException().GetType().Name),
                ("exceptionMessage", ex.Message),
                ("baseExceptionMessage", ex.GetBaseException().Message));
        }
    }
}
