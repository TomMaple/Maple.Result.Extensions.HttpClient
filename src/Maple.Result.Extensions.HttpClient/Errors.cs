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

namespace Maple.Result.Extensions.HttpClient;

/// <summary>
///     Central definitions of the <see cref="Error" /> instances produced by this library.
/// </summary>
internal static class Errors
{
    /// <summary>
    ///     The <see cref="Error" /> instances produced when JSON response content cannot be deserialized.
    /// </summary>
    internal static class Deserialization
    {
        /// <summary>
        ///     The error returned when an HTTP response body cannot be deserialized to the expected type,
        ///     whether because deserialization threw (<paramref name="exception" /> is provided) or produced no value.
        /// </summary>
        /// <param name="targetTypeName">The name of the type the error response content was being deserialized to.</param>
        /// <param name="exception">The exception thrown while deserializing, if any.</param>
        public static Error CreateUnprocessableErrorJson(string targetTypeName, Exception? exception = null)
        {
            var detailErrorPart = exception is not null
                ? $" Error: “{exception.Message}”."
                : null;

            var detailNamedValues = exception is not null
                ? new (string, object)[]
                {
                    ("exceptionType", exception.GetType().FullName ?? exception.GetType().Name),
                    ("baseExceptionType", exception.GetBaseException().GetType().FullName ?? exception.GetBaseException().GetType().Name),
                    ("exceptionMessage", exception.Message),
                    ("baseExceptionMessage", exception.GetBaseException().Message)
                }
                : Array.Empty<(string, object)>();

            return Error.Failure(
                ErrorUri.Tag("tag:mapledev.engineer,2026-03-03:result.httpClient.errorResponse.json.invalid"),
                $"Failed to deserialize the HTTP error response content.",
                $"Failed to deserialize the HTTP error response content to the {targetTypeName}.{detailErrorPart}",
                null,
                "maple.result.httpClient.json.deserialization.error",
                detailNamedValues);
        }

        /// <summary>
        ///     The error returned when an HTTP response body cannot be deserialized to the expected type,
        ///     whether because deserialization threw (<paramref name="exception" /> is provided) or produced no value.
        /// </summary>
        /// <param name="targetTypeName">The name of the type the response content was being deserialized to.</param>
        /// <param name="exception">The exception thrown while deserializing, if any.</param>
        public static Error CreateUnprocessableSuccessJson(string targetTypeName, Exception? exception = null)
        {
            var detailErrorPart = exception is not null
                ? $" Error: “{exception.Message}”."
                : null;

            var detailNamedValues = exception is not null
                ? new (string, object)[]
                {
                    ("exceptionType", exception.GetType().FullName ?? exception.GetType().Name),
                    ("baseExceptionType", exception.GetBaseException().GetType().FullName ?? exception.GetBaseException().GetType().Name),
                    ("exceptionMessage", exception.Message),
                    ("baseExceptionMessage", exception.GetBaseException().Message)
                }
                : Array.Empty<(string, object)>();

            return Error.Failure(
                ErrorUri.Tag("tag:mapledev.engineer,2026-03-03:result.httpClient.successResponse.json.invalid"),
                "Failed to deserialize the HTTP response content.",
                $"Failed to deserialize the HTTP response content to the {targetTypeName}.{detailErrorPart}",
                null,
                "maple.result.httpClient.json.deserialization.error",
                detailNamedValues);
        }
    }
}
