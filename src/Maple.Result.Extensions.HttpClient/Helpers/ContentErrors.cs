// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Net.Http;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Creates the errors that this package returns when the content of a response cannot be turned into a result (see
///     <see cref="HttpClientErrorUris" />).
/// </summary>
/// <remarks>
///     Each method returns a builder: the entries about the response and <see cref="HttpResultOptions.ConfigureError" /> are applied
///     to it before the error is built.
/// </remarks>
internal static class ContentErrors
{
    /// <summary>
    ///     The detail of <see cref="HttpClientErrorUris.InvalidSuccessContent" /> for a response without content.
    /// </summary>
    internal const string NoContentDetail = "The response has no content.";

    /// <summary>
    ///     The detail of <see cref="HttpClientErrorUris.InvalidSuccessContent" /> for the <i>JSON</i> value <c>null</c>.
    /// </summary>
    internal const string NullContentDetail = "The content of the response is the JSON value ‘null’.";

    /// <summary>
    ///     Creates the error <see cref="HttpClientErrorUris.InvalidSuccessContent" /> of a success response whose content is missing or
    ///     <c>null</c>.
    /// </summary>
    internal static ErrorBuilder CreateInvalidSuccessContent(string detail)
    {
        var builder = ErrorBuilder.Failure(HttpClientErrorUris.InvalidSuccessContent, HttpClientErrorTitles.InvalidSuccessContent);

        return builder.WithDetail(detail);
    }

    /// <summary>
    ///     Creates the error <see cref="HttpClientErrorUris.InvalidSuccessContent" /> of a success response whose content could not be
    ///     deserialized.
    /// </summary>
    internal static ErrorBuilder CreateInvalidSuccessContent(Exception exception)
    {
        var builder = ErrorBuilder.Failure(HttpClientErrorUris.InvalidSuccessContent, HttpClientErrorTitles.InvalidSuccessContent);

        return WithException(builder, exception);
    }

    /// <summary>
    ///     Creates the error <see cref="HttpClientErrorUris.UnreadableContent" /> of a response whose content could not be read.
    /// </summary>
    internal static ErrorBuilder CreateUnreadableContent(Exception exception)
    {
        var builder = ErrorBuilder.Unavailable(HttpClientErrorUris.UnreadableContent, HttpClientErrorTitles.UnreadableContent);
        WithException(builder, exception);

        var requestError = GetHttpRequestError(exception);
        if (requestError is null or HttpRequestError.Unknown)
            return builder;

        var requestErrorName = requestError.Value.ToString();

        return builder.WithMetadata(HttpErrorMetadataKeys.HttpRequestError, requestErrorName);
    }

    #region helper methods

    private static HttpRequestError? GetHttpRequestError(Exception exception)
    {
        return exception switch
        {
            HttpIOException ioException => ioException.HttpRequestError,
            HttpRequestException requestException => requestException.HttpRequestError,
            _ => null
        };
    }

    /// <summary>
    ///     Stores the message of the exception as the detail (the core rejects a blank one) and its type in the metadata.
    /// </summary>
    private static ErrorBuilder WithException(ErrorBuilder builder, Exception exception)
    {
        var message = exception.Message;
        var detail = string.IsNullOrWhiteSpace(message)
            ? null
            : message;

        var exceptionType = exception.GetType();
        var exceptionTypeName = exceptionType.ToString();

        return builder
            .WithDetail(detail)
            .WithMetadata(HttpErrorMetadataKeys.ExceptionType, exceptionTypeName);
    }

    #endregion
}
