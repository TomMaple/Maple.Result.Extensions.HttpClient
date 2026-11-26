// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Maple.Results.Extensions.HttpClient.Mappers;

/// <summary>
///     Turns the body of an error response into the builder of its error.
/// </summary>
/// <remarks>
///     The entries about the response (<see cref="HttpErrorMetadataKeys" />) and <see cref="HttpResultOptions.ConfigureError" /> are
///     applied to the builder afterwards, so they win over what a mapper stores.
/// </remarks>
internal abstract class ErrorMapper
{
    /// <summary>
    ///     Returns the builder of the error of the response that the <paramref name="context" /> describes.
    /// </summary>
    internal abstract ErrorBuilder CreateBuilder(HttpErrorContext context, HttpResultOptions options);

    /// <summary>
    ///     Returns a builder with the error of the status code: its category, the type <c>about:blank</c> and its phrase as the title.
    /// </summary>
    protected static ErrorBuilder CreateDefaultBuilder(HttpErrorContext context)
    {
        return new ErrorBuilder(context.DefaultCategory, ErrorUri.None, context.DefaultTitle);
    }
}
