// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Net;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class ErrorCategoryMapper
{
    internal static ErrorCategory Map(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => ErrorCategory.Validation,
            HttpStatusCode.Unauthorized => ErrorCategory.Unauthenticated,
            HttpStatusCode.Forbidden => ErrorCategory.Unauthorized,
            HttpStatusCode.NotFound => ErrorCategory.NotFound,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ErrorCategory.Timeout,
            HttpStatusCode.Conflict => ErrorCategory.Conflict,
            HttpStatusCode.UnprocessableContent => ErrorCategory.Failure,
            HttpStatusCode.InternalServerError => ErrorCategory.Critical,
            HttpStatusCode.NotImplemented => ErrorCategory.NotImplemented,
            HttpStatusCode.ServiceUnavailable => ErrorCategory.Unavailable,
            _ => ErrorCategory.Failure
        };
    }
}
