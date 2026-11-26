// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Results.Extensions.HttpClient.Helpers;
using Maple.Results.Extensions.HttpClient.Mappers;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Maple.Results.Extensions.HttpClient;

/// <summary>
///     Extension methods that map an <see cref="HttpResponseMessage" /> to a <see cref="Result" /> or a <see cref="Result{T}" />.
/// </summary>
/// <remarks>
///     <para>
///         A response with a success status code is a successful result; any other status code is a failed result whose
///         <see cref="Error" /> has the category of the status code (see <see cref="HttpResultOptions.DefaultStatusMap" />).
///     </para>
///     <para>
///         The methods map a response that the caller already has: they do not send a request, and they do not dispose the response.
///         Only a failure while the content is read becomes an error (<see cref="HttpClientErrorUris.UnreadableContent" />). A failure
///         before there is a response (e.g., no connection) stays the exception of <see cref="System.Net.Http.HttpClient" />, and a
///         cancellation always stays an <see cref="OperationCanceledException" />.
///     </para>
///     <para>
///         The content is read to its end, and its stream is disposed. A response whose content was buffered (the default of
///         <see cref="System.Net.Http.HttpClient" />) can still be read by the caller afterwards; one that was sent with
///         <see cref="HttpCompletionOption.ResponseHeadersRead" /> cannot.
///     </para>
/// </remarks>
public static class HttpResponseMessageExtensions
{
    #region the error of the status code

    /// <summary>
    ///     Maps the response to a <see cref="Result" />: a success for a success status code, otherwise the error of the status code.
    /// </summary>
    /// <param name="response">The response to map; it is not disposed.</param>
    /// <param name="options">
    ///     The settings of the mapping; when <see langword="null" />, the instance stored on the request of the response under
    ///     <see cref="HttpResultOptions.RequestOptionsKey" />, or else <see cref="HttpResultOptions.Default" />. The instance becomes
    ///     read-only.
    /// </param>
    /// <param name="cancellationToken">The token that cancels the reading of the content.</param>
    /// <returns>
    ///     A task with a successful result for a success status code (the content is not read), or with a failed result for any other
    ///     status code.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The error of a status code has the category that <see cref="HttpResultOptions.MapStatus" /> or
    ///         <see cref="HttpResultOptions.DefaultStatusMap" /> gives, the type <c>about:blank</c> (<see cref="ErrorUri.None" />) and the
    ///         phrase of the status code as its title (e.g., “Not Found”; never the reason phrase or the body of the response). The body
    ///         does not change this error; to read it, use an overload with a <c>mapError</c> function.
    ///     </para>
    ///     <para>
    ///         Every error has the status code of the response in its metadata (<see cref="HttpErrorMetadataKeys.HttpStatus" />, read with
    ///         <see cref="ErrorHttpExtensions.TryGetHttpStatus" />), and <see cref="HttpResultOptions.ConfigureError" /> runs last.
    ///     </para>
    ///     <para>
    ///         When the content of an error response cannot be read (e.g., the connection drops after the headers), the result is the
    ///         error <see cref="HttpClientErrorUris.UnreadableContent" /> of the category <see cref="ErrorCategory.Unavailable" />.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If the <paramref name="response" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">
    ///     If the <paramref name="cancellationToken" /> is canceled; a cancellation is never turned into an error.
    /// </exception>
    /// <exception cref="ObjectDisposedException">If the content of the <paramref name="response" /> has to be read and was disposed.</exception>
    public static Task<Result> ToResultAsync(this HttpResponseMessage response, HttpResultOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return ToResultCoreAsync(response, options, DefaultErrorMapper.Instance, cancellationToken);
    }

    /// <summary>
    ///     Maps the response to a <see cref="Result{T}" />: the content read as <typeparamref name="T" /> for a success status code,
    ///     otherwise the error of the status code.
    /// </summary>
    /// <typeparam name="T">The type of the value of a successful response.</typeparam>
    /// <returns>
    ///     A task with a successful result that has the value for a success status code, or with a failed result for any other status
    ///     code and for content that is not a value.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The content of a response with a success status code is read as <i>JSON</i> with
    ///         <see cref="HttpResultOptions.JsonSerializerOptions" />, whatever its media type. Only when <typeparamref name="T" /> is
    ///         <see cref="string" /> and the media type is a text one (<c>text/*</c>), the content is the value as it is.
    ///     </para>
    ///     <para>
    ///         A successful result always has a value. When the response has no content, or its content is the <i>JSON</i> value
    ///         <c>null</c> or cannot be read as a <typeparamref name="T" />, the result is the error
    ///         <see cref="HttpClientErrorUris.InvalidSuccessContent" /> of the category <see cref="ErrorCategory.Failure" />, with the
    ///         type of the exception in its metadata (<see cref="HttpErrorMetadataKeys.ExceptionType" />). When the content cannot be
    ///         read at all, it is the error <see cref="HttpClientErrorUris.UnreadableContent" />.
    ///     </para>
    ///     <para>
    ///         Any other status code gives the error that
    ///         <see cref="ToResultAsync(HttpResponseMessage, HttpResultOptions, CancellationToken)" /> describes.
    ///     </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    ///     If the <see cref="HttpResultOptions.JsonSerializerOptions" /> have no metadata of <typeparamref name="T" /> (e.g., a
    ///     source-generated context that does not name the type); this is not turned into an error.
    /// </exception>
    /// <inheritdoc cref="ToResultAsync(HttpResponseMessage, HttpResultOptions, CancellationToken)" />
    public static Task<Result<T>> ToResultAsync<T>(this HttpResponseMessage response, HttpResultOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return ToResultCoreAsync<T>(response, options, DefaultErrorMapper.Instance, cancellationToken);
    }

    #endregion

    #region a function that changes the builder of the error

    /// <summary>
    ///     Maps the response to a <see cref="Result" />; the error of an error response is the error of the status code, changed by a
    ///     function that gets the body read as <typeparamref name="TError" />.
    /// </summary>
    /// <typeparam name="TError">The type of the body of an error response.</typeparam>
    /// <param name="response">The response to map; it is not disposed.</param>
    /// <param name="mapError">
    ///     The function that changes the error. It gets the body, the context of the response, and a builder that has the error of the
    ///     status code: its category, the type <c>about:blank</c> and its phrase as the title.
    /// </param>
    /// <param name="options">
    ///     The settings of the mapping; when <see langword="null" />, the instance stored on the request of the response under
    ///     <see cref="HttpResultOptions.RequestOptionsKey" />, or else <see cref="HttpResultOptions.Default" />. The instance becomes
    ///     read-only.
    /// </param>
    /// <param name="cancellationToken">The token that cancels the reading of the content.</param>
    /// <returns>
    ///     A task with a successful result for a success status code (the content is not read), or with a failed result for any other
    ///     status code.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The body is read as <i>JSON</i> with <see cref="HttpResultOptions.JsonSerializerOptions" />. A body that is empty or not a
    ///         <typeparamref name="TError" /> is not an error by itself: the function gets <see langword="null" /> (the default value of
    ///         a value type) and can read the text in <see cref="HttpErrorContext.Content" />.
    ///     </para>
    ///     <para>
    ///         After the function, the status code of the response is stored in the metadata
    ///         (<see cref="HttpErrorMetadataKeys.HttpStatus" />; it replaces an entry that the function stored under that key), and
    ///         <see cref="HttpResultOptions.ConfigureError" /> runs.
    ///     </para>
    ///     <para>
    ///         An exception that the function throws is not caught. The function is not called for a success status code, nor when the
    ///         content cannot be read: the result is the error <see cref="HttpClientErrorUris.UnreadableContent" /> then.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     If the <paramref name="response" /> or the <paramref name="mapError" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">
    ///     If the <see cref="HttpResultOptions.JsonSerializerOptions" /> have no metadata of a type that has to be read (e.g., a
    ///     source-generated context that does not name the type); this is not turned into an error.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///     If the <paramref name="cancellationToken" /> is canceled; a cancellation is never turned into an error.
    /// </exception>
    /// <exception cref="ObjectDisposedException">If the content of the <paramref name="response" /> has to be read and was disposed.</exception>
    public static Task<Result> ToResultAsync<TError>(this HttpResponseMessage response, Action<TError?, HttpErrorContext, ErrorBuilder> mapError,
        HttpResultOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(mapError);

        var errorMapper = new BuilderErrorMapper<TError>(mapError);

        return ToResultCoreAsync(response, options, errorMapper, cancellationToken);
    }

    /// <summary>
    ///     Maps the response to a <see cref="Result{T}" />: the content read as <typeparamref name="T" /> for a success status code;
    ///     the error of an error response is the error of the status code, changed by a function that gets the body read as
    ///     <typeparamref name="TError" />.
    /// </summary>
    /// <typeparam name="T">The type of the value of a successful response.</typeparam>
    /// <typeparam name="TError">The type of the body of an error response.</typeparam>
    /// <returns>
    ///     A task with a successful result that has the value for a success status code, or with a failed result for any other status
    ///     code and for content that is not a value.
    /// </returns>
    /// <remarks>
    ///     The value is read as <see cref="ToResultAsync{T}(HttpResponseMessage, HttpResultOptions, CancellationToken)" /> describes, and
    ///     the error is mapped as
    ///     <see cref="ToResultAsync{TError}(HttpResponseMessage, Action{TError, HttpErrorContext, ErrorBuilder}, HttpResultOptions, CancellationToken)" />
    ///     describes.
    /// </remarks>
    /// <inheritdoc cref="ToResultAsync{TError}(HttpResponseMessage, Action{TError, HttpErrorContext, ErrorBuilder}, HttpResultOptions, CancellationToken)" />
    public static Task<Result<T>> ToResultAsync<T, TError>(this HttpResponseMessage response,
        Action<TError?, HttpErrorContext, ErrorBuilder> mapError, HttpResultOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(mapError);

        var errorMapper = new BuilderErrorMapper<TError>(mapError);

        return ToResultCoreAsync<T>(response, options, errorMapper, cancellationToken);
    }

    #endregion

    #region a function that returns the error

    /// <summary>
    ///     Maps the response to a <see cref="Result" />; the error of an error response is the one that a function returns for the
    ///     body read as <typeparamref name="TError" />.
    /// </summary>
    /// <typeparam name="TError">The type of the body of an error response.</typeparam>
    /// <param name="response">The response to map; it is not disposed.</param>
    /// <param name="mapError">
    ///     The function that returns the error. It gets the body and the context of the response, which has the category and the title
    ///     of the status code (<see cref="HttpErrorContext.DefaultCategory" />, <see cref="HttpErrorContext.DefaultTitle" />).
    /// </param>
    /// <param name="options">
    ///     The settings of the mapping; when <see langword="null" />, the instance stored on the request of the response under
    ///     <see cref="HttpResultOptions.RequestOptionsKey" />, or else <see cref="HttpResultOptions.Default" />. The instance becomes
    ///     read-only.
    /// </param>
    /// <param name="cancellationToken">The token that cancels the reading of the content.</param>
    /// <returns>
    ///     A task with a successful result for a success status code (the content is not read), or with a failed result for any other
    ///     status code.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The body is read as <i>JSON</i> with <see cref="HttpResultOptions.JsonSerializerOptions" />. A body that is empty or not a
    ///         <typeparamref name="TError" /> is not an error by itself: the function gets <see langword="null" /> (the default value of
    ///         a value type) and can read the text in <see cref="HttpErrorContext.Content" />.
    ///     </para>
    ///     <para>
    ///         The result has a copy of the returned error with the status code of the response in its metadata
    ///         (<see cref="HttpErrorMetadataKeys.HttpStatus" />; it replaces an entry of that key), changed by
    ///         <see cref="HttpResultOptions.ConfigureError" />. The category is the one of the returned error.
    ///     </para>
    ///     <para>
    ///         An exception that the function throws is not caught. The function is not called for a success status code, nor when the
    ///         content cannot be read: the result is the error <see cref="HttpClientErrorUris.UnreadableContent" /> then.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     If the <paramref name="response" /> or the <paramref name="mapError" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">If the <paramref name="mapError" /> function returns <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">
    ///     If the <see cref="HttpResultOptions.JsonSerializerOptions" /> have no metadata of a type that has to be read (e.g., a
    ///     source-generated context that does not name the type); this is not turned into an error.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///     If the <paramref name="cancellationToken" /> is canceled; a cancellation is never turned into an error.
    /// </exception>
    /// <exception cref="ObjectDisposedException">If the content of the <paramref name="response" /> has to be read and was disposed.</exception>
    public static Task<Result> ToResultAsync<TError>(this HttpResponseMessage response, Func<TError?, HttpErrorContext, Error> mapError,
        HttpResultOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(mapError);

        var errorMapper = new FunctionErrorMapper<TError>(mapError);

        return ToResultCoreAsync(response, options, errorMapper, cancellationToken);
    }

    /// <summary>
    ///     Maps the response to a <see cref="Result{T}" />: the content read as <typeparamref name="T" /> for a success status code;
    ///     the error of an error response is the one that a function returns for the body read as <typeparamref name="TError" />.
    /// </summary>
    /// <typeparam name="T">The type of the value of a successful response.</typeparam>
    /// <typeparam name="TError">The type of the body of an error response.</typeparam>
    /// <returns>
    ///     A task with a successful result that has the value for a success status code, or with a failed result for any other status
    ///     code and for content that is not a value.
    /// </returns>
    /// <remarks>
    ///     The value is read as <see cref="ToResultAsync{T}(HttpResponseMessage, HttpResultOptions, CancellationToken)" /> describes, and
    ///     the error is mapped as
    ///     <see cref="ToResultAsync{TError}(HttpResponseMessage, Func{TError, HttpErrorContext, Error}, HttpResultOptions, CancellationToken)" />
    ///     describes.
    /// </remarks>
    /// <inheritdoc cref="ToResultAsync{TError}(HttpResponseMessage, Func{TError, HttpErrorContext, Error}, HttpResultOptions, CancellationToken)" />
    public static Task<Result<T>> ToResultAsync<T, TError>(this HttpResponseMessage response, Func<TError?, HttpErrorContext, Error> mapError,
        HttpResultOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(mapError);

        var errorMapper = new FunctionErrorMapper<TError>(mapError);

        return ToResultCoreAsync<T>(response, options, errorMapper, cancellationToken);
    }

    #endregion

    #region helper methods

    /// <summary>
    ///     Applies what every error of a response gets: the entries about the response, which replace entries of the same keys, and
    ///     then <see cref="HttpResultOptions.ConfigureError" />.
    /// </summary>
    private static Error Complete(ErrorBuilder builder, HttpErrorContext context, HttpResultOptions options)
    {
        var statusCode = (int)context.StatusCode;
        builder.WithMetadata(HttpErrorMetadataKeys.HttpStatus, statusCode);

        options.ConfigureError?.Invoke(context, builder);

        return builder.Build();
    }

    /// <summary>
    ///     Creates the context of the response. A success status code never reaches the status map: the errors of such a response are
    ///     about its content.
    /// </summary>
    private static HttpErrorContext CreateContext(HttpResponseMessage response, HttpResultOptions options, string content)
    {
        var statusCode = response.StatusCode;
        var defaultCategory = response.IsSuccessStatusCode
            ? ErrorCategory.Failure
            : options.GetCategory(statusCode);

        var defaultTitle = HttpStatusPhrases.Get(statusCode);

        return new HttpErrorContext(response, content, defaultCategory, defaultTitle);
    }

    /// <summary>
    ///     Whether the exception is a failure of reading the content, which becomes an error; a cancellation, a disposed response and
    ///     any other exception are not.
    /// </summary>
    private static bool IsReadFailure(Exception exception)
    {
        return exception is IOException or HttpRequestException;
    }

    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response, HttpResultOptions options, ErrorMapper errorMapper,
        CancellationToken cancellationToken)
    {
        string content;
        try
        {
            content = await HttpContentReader.ReadTextAsync(response.Content, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            var failureContext = CreateContext(response, options, string.Empty);
            var failureBuilder = ContentErrors.UnreadableContent(exception);

            return Complete(failureBuilder, failureContext, options);
        }

        var context = CreateContext(response, options, content);
        var builder = errorMapper.CreateBuilder(context, options);

        return Complete(builder, context, options);
    }

    /// <summary>
    ///     Whether the content is the value as it is: a text content for a <see cref="string" />. Any other content is <i>JSON</i>.
    /// </summary>
    private static bool ReadsRawText<T>(HttpContent content)
    {
        return typeof(T) == typeof(string) && HttpContentReader.IsText(content);
    }

    private static async Task<Result<T>> ReadValueAsync<T>(HttpResponseMessage response, HttpResultOptions options,
        CancellationToken cancellationToken)
    {
        var content = response.Content;
        ErrorBuilder errorBuilder;

        try
        {
            if (HttpContentReader.IsEmpty(content))
            {
                errorBuilder = ContentErrors.InvalidSuccessContent(ContentErrors.NoContentDetail);
            }
            else if (ReadsRawText<T>(content))
            {
                var text = await HttpContentReader.ReadTextAsync(content, cancellationToken).ConfigureAwait(false);
                if (text.Length > 0)
                {
                    var textValue = (T)(object)text;

                    return Result<T>.FromValue(textValue);
                }

                errorBuilder = ContentErrors.InvalidSuccessContent(ContentErrors.NoContentDetail);
            }
            else
            {
                var jsonTypeInfo = JsonReader.GetTypeInfo<T>(options.JsonSerializerOptions);
                var value = await HttpContentReader.ReadJsonAsync(content, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
                if (value is not null)
                    return Result<T>.FromValue(value);

                errorBuilder = ContentErrors.InvalidSuccessContent(ContentErrors.NullContentDetail);
            }
        }
        catch (JsonException exception)
        {
            errorBuilder = ContentErrors.InvalidSuccessContent(exception);
        }
        catch (Exception exception) when (IsReadFailure(exception))
        {
            errorBuilder = ContentErrors.UnreadableContent(exception);
        }

        var context = CreateContext(response, options, string.Empty);
        var error = Complete(errorBuilder, context, options);

        return Result<T>.FromError(error);
    }

    private static async Task<Result> ToResultCoreAsync(HttpResponseMessage response, HttpResultOptions? options, ErrorMapper errorMapper,
        CancellationToken cancellationToken)
    {
        var resolvedOptions = HttpResultOptions.Resolve(response, options);
        if (response.IsSuccessStatusCode)
            return Result.Success();

        var error = await ReadErrorAsync(response, resolvedOptions, errorMapper, cancellationToken).ConfigureAwait(false);

        return Result.FromError(error);
    }

    private static async Task<Result<T>> ToResultCoreAsync<T>(HttpResponseMessage response, HttpResultOptions? options, ErrorMapper errorMapper,
        CancellationToken cancellationToken)
    {
        var resolvedOptions = HttpResultOptions.Resolve(response, options);
        if (response.IsSuccessStatusCode)
            return await ReadValueAsync<T>(response, resolvedOptions, cancellationToken).ConfigureAwait(false);

        var error = await ReadErrorAsync(response, resolvedOptions, errorMapper, cancellationToken).ConfigureAwait(false);

        return Result<T>.FromError(error);
    }

    #endregion
}
