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

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class ErrorUriMapper
{
    #region consts

    private const string NoneValue = "about:blank";

    #endregion

    internal static ErrorUri? Map(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        try
        {
            if (IsUriLocator(source))
                return ErrorUri.Locator(source);

            if (IsUriTag(source))
                return ErrorUri.Tag(source);
        }
        catch
        {
            return null;
        }

        return null;
    }

    internal static ErrorUri MapTypeUri(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return ErrorUri.None();

        if (string.Equals(source, NoneValue, StringComparison.InvariantCultureIgnoreCase))
            return ErrorUri.None();

        return Map(source)
               ?? ErrorUri.None();
    }

    private static bool IsUriLocator(string source)
    {
        return source.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) 
            || source.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsUriTag(string source)
    {
        return source.StartsWith("tag:", StringComparison.InvariantCultureIgnoreCase);
    }
}
