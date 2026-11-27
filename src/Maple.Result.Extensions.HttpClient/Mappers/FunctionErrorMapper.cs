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
using System;

namespace Maple.Results.Extensions.HttpClient.Mappers;

/// <summary>
///     The mapper of a function of the caller that returns the whole error from the body read as <typeparamref name="TError" />.
/// </summary>
/// <typeparam name="TError">The type of the body of an error response.</typeparam>
internal sealed class FunctionErrorMapper<TError> : ErrorMapper
{
    #region consts

    // The name of the parameter of the caller, as the core names the function in the same message.
    private const string FunctionName = "mapError";

    #endregion

    private readonly Func<TError?, HttpErrorContext, Error> _mapError;

    #region constructors

    internal FunctionErrorMapper(Func<TError?, HttpErrorContext, Error> mapError)
    {
        _mapError = mapError;
    }

    #endregion

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">If the function returns <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">If the options have no metadata of <typeparamref name="TError" />.</exception>
    internal override ErrorBuilder CreateBuilder(HttpErrorContext context, HttpResultOptions options)
    {
        var body = JsonReader.ReadOrDefault<TError>(context.Content, options.JsonSerializerOptions);
        var error = _mapError(body, context);
        if (error is null)
            throw new InvalidOperationException($"The function ‘{FunctionName}’ returned null.");

        return error.ToBuilder();
    }
}
