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
///     The mapper of a function of the caller that changes a builder: the builder has the error of the status code, and the function
///     gets the body read as <typeparamref name="TError" />.
/// </summary>
/// <typeparam name="TError">The type of the body of an error response.</typeparam>
internal sealed class BuilderErrorMapper<TError> : ErrorMapper
{
    private readonly Action<TError?, HttpErrorContext, ErrorBuilder> _mapError;

    #region constructors

    internal BuilderErrorMapper(Action<TError?, HttpErrorContext, ErrorBuilder> mapError)
    {
        _mapError = mapError;
    }

    #endregion

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">If the options have no metadata of <typeparamref name="TError" />.</exception>
    internal override ErrorBuilder CreateBuilder(HttpErrorContext context, HttpResultOptions options)
    {
        var body = JsonReader.ReadOrDefault<TError>(context.Content, options.JsonSerializerOptions);
        var builder = CreateDefaultBuilder(context);

        _mapError(body, context, builder);

        return builder;
    }
}
