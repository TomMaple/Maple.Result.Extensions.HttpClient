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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient;

/// <summary>
///     The settings of the mapping of an <see cref="HttpResponseMessage" /> to a result.
/// </summary>
/// <remarks>
///     <para>
///         Pass an instance to a <c>ToResultAsync</c> method of <see cref="HttpResponseMessageExtensions" />, or set it on the request under
///         <see cref="RequestOptionsKey" />, so that every response of that request uses it. A call without either uses
///         <see cref="Default" />.
///     </para>
///     <para>
///         An instance can be changed until it is first used or <see cref="MakeReadOnly" /> is called; after that, the setters throw an
///         <see cref="InvalidOperationException" />, as those of <see cref="System.Text.Json.JsonSerializerOptions" /> do. A read-only
///         instance is safe to share between threads and requests. There is no global setting: create an instance once (e.g., in a
///         static field) and reuse it.
///     </para>
/// </remarks>
public sealed class HttpResultOptions
{
    #region consts

    private const string ReadOnlyMessage =
        "The options are read-only, because they were used or MakeReadOnly was called; create a new instance to change a setting.";

    private const string ReflectionJustification =
        "The reflection-based resolver is used only where reflection-based serialization is enabled (JsonSerializer.IsReflectionEnabledByDefault). "
        + "A trimmed or Native AOT application disables it by default: the options need a source-generated resolver then, and a type without "
        + "metadata is reported with a NotSupportedException that names it.";

    private const string RequestOptionsKeyName = "Maple.Results.Extensions.HttpClient.HttpResultOptions";

    #endregion

    private volatile bool _isReadOnly;

    /// <summary>
    ///     Gets or sets a function that changes every error before it is returned, e.g., to add a correlation identifier from a header
    ///     of the response.
    /// </summary>
    /// <value>The function, or <see langword="null" /> (the default) for none.</value>
    /// <remarks>
    ///     It runs last: after the body was mapped and after the entries of <see cref="HttpErrorMetadataKeys" /> were added, so what it
    ///     sets is kept. It runs for every error that a <c>ToResultAsync</c> method returns, also for the errors of
    ///     <see cref="HttpClientErrors" />. An exception that it throws is not caught. It may run on a thread-pool thread, without the
    ///     <see cref="System.Threading.SynchronizationContext" /> of the caller (see <see cref="HttpResponseMessageExtensions" />).
    /// </remarks>
    /// <exception cref="InvalidOperationException">If the value is set on a read-only instance.</exception>
    public Action<HttpErrorContext, ErrorBuilder>? ConfigureError
    {
        get;
        set
        {
            EnsureMutable();
            field = value;
        }
    }

    /// <summary>
    ///     Gets the read-only instance with the default settings, used by a call that has no options of its own.
    /// </summary>
    public static HttpResultOptions Default { get; } = CreateDefault();

    /// <summary>
    ///     Gets or sets the options that read the content of a response as <i>JSON</i>: the value of a successful result and the body of
    ///     an error as a type of the caller.
    /// </summary>
    /// <value>The options; the default is <see cref="System.Text.Json.JsonSerializerOptions.Web" />.</value>
    /// <remarks>
    ///     <para>
    ///         The types are read through <see cref="System.Text.Json.JsonSerializerOptions.GetTypeInfo(Type)" />. Where reflection-based
    ///         serialization is enabled (the default, except in a trimmed or Native AOT application), any type can be read, and options
    ///         without a <see cref="System.Text.Json.JsonSerializerOptions.TypeInfoResolver" /> get the reflection-based one, as the
    ///         <see cref="JsonSerializer" /> does.
    ///     </para>
    ///     <para>
    ///         A trimmed or Native AOT application sets options with a source-generated resolver (a
    ///         <see cref="System.Text.Json.Serialization.JsonSerializerContext" />) that has the types it reads. A type without metadata
    ///         is reported with a <see cref="NotSupportedException" />.
    ///     </para>
    ///     <para>The options become read-only together with this instance.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If the value is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">If the value is set on a read-only instance.</exception>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureMutable();

            field = value;
        }
    } = GetWebJsonSerializerOptions();

    /// <summary>
    ///     Gets or sets a function that chooses the category of an error from the status code of the response.
    /// </summary>
    /// <value>The function, or <see langword="null" /> (the default) to use <see cref="GetDefaultCategory" /> for every status code.</value>
    /// <remarks>
    ///     When the function returns <see langword="null" />, <see cref="GetDefaultCategory" /> decides, so it has to name only the status
    ///     codes that it reads differently. It is not called for a success status code. The status code of the response decides the
    ///     category; the <c>status</c> member of a problem details body is not read (it is advisory, <i>RFC 9457</i>, section 3.1.2). It
    ///     may run on a thread-pool thread, without the <see cref="System.Threading.SynchronizationContext" /> of the caller (see
    ///     <see cref="HttpResponseMessageExtensions" />).
    /// </remarks>
    /// <exception cref="InvalidOperationException">If the value is set on a read-only instance.</exception>
    public Func<HttpStatusCode, ErrorCategory?>? MapStatus
    {
        get;
        set
        {
            EnsureMutable();
            field = value;
        }
    }

    /// <summary>
    ///     Gets the key under which an instance is stored in the options of a request (<see cref="HttpRequestMessage.Options" />), for the
    ///     responses of that request.
    /// </summary>
    /// <remarks>
    ///     A <c>ToResultAsync</c> method uses its <c>options</c> argument; without one, the instance of the request of the response;
    ///     without that, <see cref="Default" />.
    /// </remarks>
    public static HttpRequestOptionsKey<HttpResultOptions> RequestOptionsKey { get; } = new(RequestOptionsKeyName);

    /// <summary>
    ///     Returns the category that a status code of an error response is read as by default.
    /// </summary>
    /// <param name="statusCode">The status code of the response.</param>
    /// <returns>The category.</returns>
    /// <remarks>
    ///     <para>
    ///         400, 413, 414 and 431 are <see cref="ErrorCategory.Validation" />; 401 is <see cref="ErrorCategory.Unauthenticated" />; 403 is
    ///         <see cref="ErrorCategory.PermissionDenied" />; 404 and 410 are <see cref="ErrorCategory.NotFound" />; 408 and 504 are
    ///         <see cref="ErrorCategory.Timeout" />; 409, 412 and 423 are <see cref="ErrorCategory.Conflict" />; 429 is
    ///         <see cref="ErrorCategory.LimitExceeded" />; 501 is <see cref="ErrorCategory.NotImplemented" />; 502 and 503 are
    ///         <see cref="ErrorCategory.Unavailable" />; 500 and any other 5xx code are <see cref="ErrorCategory.Critical" />; 422 and any
    ///         other code are <see cref="ErrorCategory.Failure" />.
    ///     </para>
    ///     <para>
    ///         This is the mapping of the problem details format of Maple.Result (see
    ///         <see href="https://github.com/TomMaple/Maple.Result/blob/main/docs/problem-details.md#category-and-status" />). A server
    ///         error is never read as <see cref="ErrorCategory.Failure" />, which is an expected error.
    ///     </para>
    /// </remarks>
    public static ErrorCategory GetDefaultCategory(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;

        return code switch
        {
            400 or 413 or 414 or 431 => ErrorCategory.Validation,
            401 => ErrorCategory.Unauthenticated,
            403 => ErrorCategory.PermissionDenied,
            404 or 410 => ErrorCategory.NotFound,
            408 or 504 => ErrorCategory.Timeout,
            409 or 412 or 423 => ErrorCategory.Conflict,
            429 => ErrorCategory.LimitExceeded,
            501 => ErrorCategory.NotImplemented,
            502 or 503 => ErrorCategory.Unavailable,
            >= 500 and <= 599 => ErrorCategory.Critical,
            _ => ErrorCategory.Failure
        };
    }

    /// <summary>
    ///     Makes the instance read-only, so that it can be shared safely; calling it again does nothing.
    /// </summary>
    /// <remarks>
    ///     The <see cref="JsonSerializerOptions" /> become read-only too. A <c>ToResultAsync</c> method calls this for the options that
    ///     it uses.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     If the <see cref="JsonSerializerOptions" /> have no <see cref="System.Text.Json.JsonSerializerOptions.TypeInfoResolver" /> and
    ///     reflection-based serialization is disabled (e.g., in a Native AOT application).
    /// </exception>
    public void MakeReadOnly()
    {
        if (_isReadOnly)
            return;

        MakeJsonSerializerOptionsReadOnly(JsonSerializerOptions);
        _isReadOnly = true;
    }

    #region internal methods

    /// <summary>
    ///     Returns the category of the <paramref name="statusCode" />: the one that <see cref="MapStatus" /> chooses, or the one of
    ///     <see cref="GetDefaultCategory" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">If <see cref="MapStatus" /> returns a value that is not a defined category.</exception>
    internal ErrorCategory GetCategory(HttpStatusCode statusCode)
    {
        var mapped = MapStatus?.Invoke(statusCode);
        if (mapped is null)
            return GetDefaultCategory(statusCode);

        var category = mapped.Value;
        if (!Enum.IsDefined(category))
        {
            var code = (int)statusCode;

            throw new InvalidOperationException(
                $"The function ‘{nameof(MapStatus)}’ of the options returned ‘{category}’ for the status code {code}, "
                + "which is not a defined error category.");
        }

        return category;
    }

    /// <summary>
    ///     Returns the options of a call, read-only: the <paramref name="options" /> argument; without one, the instance stored on the
    ///     request of the <paramref name="response" /> under <see cref="RequestOptionsKey" />; without that, <see cref="Default" />.
    /// </summary>
    internal static HttpResultOptions Resolve(HttpResponseMessage response, HttpResultOptions? options)
    {
        if (options is null)
        {
            var requestOptions = GetRequestOptions(response);
            options = requestOptions ?? Default;
        }

        options.MakeReadOnly();

        return options;
    }

    #endregion

    #region helper methods

    private static HttpResultOptions CreateDefault()
    {
        var options = new HttpResultOptions();
        options.MakeReadOnly();

        return options;
    }

    private void EnsureMutable()
    {
        if (_isReadOnly)
            throw new InvalidOperationException(ReadOnlyMessage);
    }

    private static HttpResultOptions? GetRequestOptions(HttpResponseMessage response)
    {
        var request = response.RequestMessage;
        if (request is null)
            return null;

        var hasOptions = request.Options.TryGetValue(RequestOptionsKey, out var options);

        return hasOptions
            ? options
            : null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = ReflectionJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = ReflectionJustification)]
    private static JsonSerializerOptions GetWebJsonSerializerOptions()
    {
        // The shared instance has the reflection-based resolver where reflection is enabled, and an empty one elsewhere.
        return JsonSerializerOptions.Web;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = ReflectionJustification)]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = ReflectionJustification)]
    private static void MakeJsonSerializerOptionsReadOnly(JsonSerializerOptions jsonSerializerOptions)
    {
        if (jsonSerializerOptions.IsReadOnly)
            return;

        // As the JsonSerializer does on first use: options without a resolver get the reflection-based one where reflection is enabled.
        // Elsewhere they are rejected, with an exception that asks for a resolver.
        var populateMissingResolver = JsonSerializer.IsReflectionEnabledByDefault;
        jsonSerializerOptions.MakeReadOnly(populateMissingResolver);
    }

    #endregion
}
