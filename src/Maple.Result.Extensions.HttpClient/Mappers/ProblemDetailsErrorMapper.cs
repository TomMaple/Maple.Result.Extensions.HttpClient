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

namespace Maple.Results.Extensions.HttpClient.Mappers;

/// <summary>
///     The mapper of the calls without a function of the caller: the error of the status code, changed by a problem details body
///     (<i>RFC 9457</i>) when the body is one.
/// </summary>
/// <remarks>See <see cref="ProblemDetailsReader" /> for which bodies are read and how.</remarks>
internal sealed class ProblemDetailsErrorMapper : ErrorMapper
{
    #region constructors

    private ProblemDetailsErrorMapper()
    {
    }

    #endregion

    /// <summary>
    ///     Gets the instance; the mapper has no state.
    /// </summary>
    internal static ProblemDetailsErrorMapper Instance { get; } = new();

    /// <inheritdoc />
    internal override ErrorBuilder CreateBuilder(HttpErrorContext context, HttpResultOptions options)
    {
        var builder = CreateDefaultBuilder(context);
        if (ProblemDetailsReader.CanRead(context.Response.Content, context.Content))
            ProblemDetailsReader.Read(context.Content, builder, context.RequestUri, options.ErrorKeyNamingPolicy);

        return builder;
    }
}
