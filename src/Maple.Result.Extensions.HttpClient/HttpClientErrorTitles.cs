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
///     The titles (<see cref="Error.Title" />) of the errors that this package creates itself; each belongs to the type of the same name
///     in <see cref="HttpClientErrorUris" />.
/// </summary>
/// <remarks>
///     The title of a problem type is the same for every occurrence (<i>RFC 9457</i>, section 3.1.4); what differs is in
///     <see cref="Error.Detail" /> and <see cref="Error.Metadata" />.
/// </remarks>
public static class HttpClientErrorTitles
{
    /// <summary>
    ///     “The HTTP response content could not be deserialized.”, the title of <see cref="HttpClientErrorUris.InvalidSuccessContent" />.
    /// </summary>
    public const string InvalidSuccessContent = "The HTTP response content could not be deserialized.";

    /// <summary>
    ///     “The HTTP response content could not be read.”, the title of <see cref="HttpClientErrorUris.UnreadableContent" />.
    /// </summary>
    public const string UnreadableContent = "The HTTP response content could not be read.";
}
