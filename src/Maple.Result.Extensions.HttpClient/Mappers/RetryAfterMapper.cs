// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Maple.Result.Extensions.HttpClient.Helpers;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class RetryAfterMapper
{
    #region consts

    private const string DetailTemplateId = "maple.result.httpClient.errorResponse.retryAfter";
    private const string RawParamName = "retryAfter";
    private const string SecondsParamName = "retryAfterSeconds";
    private const string DateParamName = "retryAfterDate";

    #endregion

    internal static void Map(HttpStatusCode statusCode, RetryConditionHeaderValue? retryAfter,
        ErrorBuilder errorBuilder)
    {
        if (statusCode is not HttpStatusCode.TooManyRequests || retryAfter is null)
            return;

        errorBuilder.WithDetailParam(DetailTemplateId, RawParamName, retryAfter.ToString());

        if (retryAfter.Delta is not null)
        {
            errorBuilder.WithDetailParam(DetailTemplateId, SecondsParamName, (int)retryAfter.Delta.Value.TotalSeconds);
        }
        else if (retryAfter.Date is not null)
        {
            var textValue = retryAfter.Date.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            errorBuilder.WithDetailParam(DetailTemplateId, DateParamName, textValue);
        }
    }
}
