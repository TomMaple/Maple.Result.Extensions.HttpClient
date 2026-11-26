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
using System.Net;
using System.Net.Http;

namespace Maple.Results.Extensions.HttpClient;

/// <summary>
///     What a function that maps or configures an error may need about the response, besides its body read as a type.
/// </summary>
/// <remarks>
///     It is passed to the <c>mapError</c> functions of the <c>ToResultAsync</c> methods of <see cref="HttpResponseMessageExtensions" />
///     and to <see cref="HttpResultOptions.ConfigureError" />.
/// </remarks>
public sealed class HttpErrorContext
{
    #region constructors

    /// <summary>
    ///     Initializes a new instance of the <see cref="HttpErrorContext" /> class.
    /// </summary>
    internal HttpErrorContext(HttpResponseMessage response, string content, ErrorCategory defaultCategory, string defaultTitle)
    {
        Response = response;
        Content = content;
        DefaultCategory = defaultCategory;
        DefaultTitle = defaultTitle;
    }

    #endregion

    /// <summary>
    ///     Gets the body of an error response as text, read once.
    /// </summary>
    /// <remarks>
    ///     It is the raw text, also when it could not be read as the type of the error body. It is empty when the response has no body,
    ///     when reading it failed (<see cref="HttpClientErrorUris.UnreadableContent" />), and for a response with a success status code,
    ///     whose content is not read as text (<see cref="HttpClientErrorUris.InvalidSuccessContent" />). Use this text instead of reading
    ///     the content of the <see cref="Response" /> again.
    /// </remarks>
    public string Content { get; }

    /// <summary>
    ///     Gets the category that the status code of the response maps to.
    /// </summary>
    /// <remarks>
    ///     It is the result of <see cref="HttpResultOptions.MapStatus" />, or of <see cref="HttpResultOptions.DefaultStatusMap" /> when that
    ///     function is not set or returns <see langword="null" />. A success status code is never passed to the function: for the errors
    ///     of a response with a success status code (see <see cref="HttpClientErrorUris" />), this is <see cref="ErrorCategory.Failure" />.
    /// </remarks>
    public ErrorCategory DefaultCategory { get; }

    /// <summary>
    ///     Gets the phrase of the status code of the response (e.g., “Not Found”), the title of an error that has no title of its own.
    /// </summary>
    /// <remarks>
    ///     The phrase is the one of <i>RFC 9110</i> and of the HTTP status code registry, not the reason phrase of the response: HTTP/2
    ///     and HTTP/3 have none, and a server can send any text. A status code that is not registered has “Unknown Error”.
    /// </remarks>
    public string DefaultTitle { get; }

    /// <summary>
    ///     Gets the <i>URI</i> of the request of the response, or <see langword="null" /> when the response has no request or the request
    ///     has no <i>URI</i>.
    /// </summary>
    public Uri? RequestUri => Response.RequestMessage?.RequestUri;

    /// <summary>
    ///     Gets the response: its status code, headers, content headers and request.
    /// </summary>
    /// <remarks>The body is in <see cref="Content" />; it has been read already.</remarks>
    public HttpResponseMessage Response { get; }

    /// <summary>
    ///     Gets the status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode => Response.StatusCode;
}
