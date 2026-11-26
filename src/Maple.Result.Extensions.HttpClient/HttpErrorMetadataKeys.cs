// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Maple.Results.Extensions.HttpClient;

/// <summary>
///     The keys of the <see cref="Error.Metadata" /> entries that this package adds to an error mapped from an HTTP response.
/// </summary>
/// <remarks>
///     The keys and the types of their values are a contract, so they never change: when an error is written as problem details
///     (<i>RFC 9457</i>), each entry is a member of that object, which other services and clients read.
/// </remarks>
public static class HttpErrorMetadataKeys
{
    /// <summary>
    ///     <c>exceptionType</c>: the full name of the type of the exception (a <see cref="string" />, e.g.,
    ///     <c>System.Text.Json.JsonException</c>), on an error that this package creates for content that could not be read or
    ///     deserialized (see <see cref="HttpClientErrorUris" />).
    /// </summary>
    public const string ExceptionType = "exceptionType";

    /// <summary>
    ///     <c>httpRequestError</c>: the name of the <see cref="System.Net.Http.HttpRequestError" /> value of the exception (a
    ///     <see cref="string" />, e.g., <c>ResponseEnded</c>), on the error <see cref="HttpClientErrorUris.UnreadableContent" /> when the
    ///     exception has one.
    /// </summary>
    public const string HttpRequestError = "httpRequestError";

    /// <summary>
    ///     <c>httpStatus</c>: the status code of the response (an <see cref="int" />, e.g., <c>404</c>), on every error mapped from a
    ///     response. Read it with <see cref="ErrorHttpExtensions.TryGetHttpStatus" />.
    /// </summary>
    public const string HttpStatus = "httpStatus";
}
