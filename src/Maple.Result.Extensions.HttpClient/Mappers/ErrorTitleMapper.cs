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
using System.Text.RegularExpressions;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static partial class ErrorTitleMapper
{
    #region consts

    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalCaseRegex();

    #endregion


    internal static string Map(string? reasonPhrase, HttpStatusCode statusCode)
    {
        if (!string.IsNullOrWhiteSpace(reasonPhrase))
            return reasonPhrase.Trim();

        return GetDefaultTitle(statusCode);
    }

    private static string GetDefaultTitle(HttpStatusCode statusCode)
    {
        var enumDescription = statusCode.ToString();

        return PascalCaseToTitleCase(enumDescription);
    }

    private static string PascalCaseToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return PascalCaseRegex().Replace(input, " $1");
    }
}
