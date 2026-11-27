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

namespace Maple.Results.Extensions.HttpClient;

/// <summary>
///     The errors that this package creates itself, when the content of a response cannot be turned into a result; each nested class
///     holds the type (<see cref="Error.TypeUri" />) and the title (<see cref="Error.Title" />) of one error.
/// </summary>
/// <remarks>
///     <para>
///         An error mapped from an error response has the type that its body names, or <c>about:blank</c>; these errors are the failures
///         on the side of the client. Compare <see cref="Error.TypeUri" /> with the type of an error to recognise it, e.g.,
///         <c>error.TypeUri == HttpClientErrors.UnreadableContent.Uri</c>.
///     </para>
///     <para>
///         The types are <i>URI Tags</i> (<i>RFC 4151</i>) and a contract, so they never change. The title of a type is the same for
///         every occurrence (<i>RFC 9457</i>, section 3.1.4); what differs is in <see cref="Error.Detail" /> and
///         <see cref="Error.Metadata" />.
///     </para>
///     <para>
///         Each factory method returns a builder: the entries about the response and <see cref="HttpResultOptions.ConfigureError" /> are
///         applied to it before the error is built.
///     </para>
/// </remarks>
public static class HttpClientErrors
{
    /// <summary>
    ///     The response has a success status code, but its content is not a value of the expected type (it is empty, the <i>JSON</i> value
    ///     <c>null</c>, or cannot be deserialized).
    /// </summary>
    /// <remarks>The error has the category <see cref="ErrorCategory.Failure" />.</remarks>
    public static class InvalidSuccessContent
    {
        /// <summary>
        ///     <c>tag:mapledev.engineer,2026-03-03:result.httpClient.invalidSuccessContent</c>, the type of the error.
        /// </summary>
        public static ErrorUri Uri { get; } = ErrorUri.Tag("tag:mapledev.engineer,2026-03-03:result.httpClient.invalidSuccessContent");

        /// <summary>
        ///     “The HTTP response content could not be deserialized.”, the title of the error.
        /// </summary>
        public const string Title = "The HTTP response content could not be deserialized.";

        /// <summary>
        ///     The detail of the error for a response without content.
        /// </summary>
        internal const string NoContentDetail = "The response has no content.";

        /// <summary>
        ///     The detail of the error for the <i>JSON</i> value <c>null</c>.
        /// </summary>
        internal const string NullContentDetail = "The content of the response is the JSON value ‘null’.";

        /// <summary>
        ///     Creates the error of a success response whose content is missing or <c>null</c>.
        /// </summary>
        internal static ErrorBuilder Create(string detail)
        {
            var builder = ErrorBuilder.Failure(Uri, Title);

            return builder.WithDetail(detail);
        }

        /// <summary>
        ///     Creates the error of a success response whose content could not be deserialized.
        /// </summary>
        internal static ErrorBuilder Create(Exception exception)
        {
            var builder = ErrorBuilder.Failure(Uri, Title);

            return WithException(builder, exception);
        }
    }

    /// <summary>
    ///     The status code and the headers of the response arrived, but reading its content failed (e.g., the connection was dropped).
    /// </summary>
    /// <remarks>The error has the category <see cref="ErrorCategory.Unavailable" />: the failure is transient.</remarks>
    public static class UnreadableContent
    {
        /// <summary>
        ///     <c>tag:mapledev.engineer,2026-03-03:result.httpClient.unreadableContent</c>, the type of the error.
        /// </summary>
        public static ErrorUri Uri { get; } = ErrorUri.Tag("tag:mapledev.engineer,2026-03-03:result.httpClient.unreadableContent");

        /// <summary>
        ///     “The HTTP response content could not be read.”, the title of the error.
        /// </summary>
        public const string Title = "The HTTP response content could not be read.";

        /// <summary>
        ///     Creates the error of a response whose content could not be read.
        /// </summary>
        internal static ErrorBuilder Create(Exception exception)
        {
            var builder = ErrorBuilder.Unavailable(Uri, Title);
            WithException(builder, exception);

            var requestError = GetHttpRequestError(exception);
            if (requestError is null or HttpRequestError.Unknown)
                return builder;

            var requestErrorName = requestError.Value.ToString();

            return builder.WithMetadata(HttpErrorMetadataKeys.HttpRequestError, requestErrorName);
        }
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
