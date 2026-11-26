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
///     The types (<see cref="Error.TypeUri" />) of the errors that this package creates itself, when the content of a response cannot
///     be turned into a result.
/// </summary>
/// <remarks>
///     <para>
///         An error mapped from an error response has the type that its body names, or <c>about:blank</c>; these types are for the
///         failures on the side of the client. Compare <see cref="Error.TypeUri" /> with a property of this class to recognise one, e.g.,
///         <c>error.TypeUri == HttpClientErrorUris.UnreadableContent</c>.
///     </para>
///     <para>
///         The values are <i>URI Tags</i> (<i>RFC 4151</i>) and a contract, so they never change. Their titles are in
///         <see cref="HttpClientErrorTitles" />.
///     </para>
/// </remarks>
public static class HttpClientErrorUris
{
    /// <summary>
    ///     <c>tag:mapledev.engineer,2026-03-03:result.httpClient.invalidSuccessContent</c>: the response has a success status code, but its
    ///     content is not a value of the expected type (it is empty, the <i>JSON</i> value <c>null</c>, or cannot be deserialized).
    /// </summary>
    /// <remarks>The error has the category <see cref="ErrorCategory.Failure" />.</remarks>
    public static ErrorUri InvalidSuccessContent { get; } = ErrorUri.Tag("tag:mapledev.engineer,2026-03-03:result.httpClient.invalidSuccessContent");

    /// <summary>
    ///     <c>tag:mapledev.engineer,2026-03-03:result.httpClient.unreadableContent</c>: the status code and the headers of the response
    ///     arrived, but reading its content failed (e.g., the connection was dropped).
    /// </summary>
    /// <remarks>The error has the category <see cref="ErrorCategory.Unavailable" />: the failure is transient.</remarks>
    public static ErrorUri UnreadableContent { get; } = ErrorUri.Tag("tag:mapledev.engineer,2026-03-03:result.httpClient.unreadableContent");
}
