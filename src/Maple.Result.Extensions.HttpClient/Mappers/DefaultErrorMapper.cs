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
///     The mapper of the calls without a function of the caller: the error of the status code.
/// </summary>
internal sealed class DefaultErrorMapper : ErrorMapper
{
    #region constructors

    private DefaultErrorMapper()
    {
    }

    #endregion

    /// <summary>
    ///     Gets the instance; the mapper has no state.
    /// </summary>
    internal static DefaultErrorMapper Instance { get; } = new();

    /// <inheritdoc />
    internal override ErrorBuilder CreateBuilder(HttpErrorContext context, HttpResultOptions options)
    {
        return CreateDefaultBuilder(context);
    }
}
