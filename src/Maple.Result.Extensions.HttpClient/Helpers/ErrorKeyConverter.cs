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
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Turns a key of the <c>errors</c> object of ASP.NET Core (<c>ValidationProblemDetails</c>) into a JSON Pointer (<i>RFC 6901</i>).
/// </summary>
/// <remarks>
///     <para>
///         Two syntaxes are read: the keys of the model state of ASP.NET Core (<c>Items[0].Name</c>) and the paths of the binding errors
///         of <i>System.Text.Json</i> (<c>$.items[0].name</c>, <c>$['first name']</c>). Names are separated by <c>.</c> or quoted in
///         <c>['…']</c>, and an index is a decimal number in <c>[…]</c>; <c>$</c> and the empty key are the whole input (the empty
///         pointer).
///     </para>
///     <para>
///         A key in any other syntax (e.g., <c>Items[x]</c>) is one name. The names are escaped by <see cref="ErrorDetail.Pointer" />,
///         so the pointer is always valid.
///     </para>
/// </remarks>
internal static class ErrorKeyConverter
{
    #region consts

    private const char IndexEnd = ']';
    private const char IndexStart = '[';
    private const string QuotedNameEnd = "']";
    private const char Quote = '\'';
    private const char Root = '$';
    private const char Separator = '.';

    #endregion

    /// <summary>
    ///     Returns the pointer of the key, with each name changed by the <paramref name="namingPolicy" />.
    /// </summary>
    /// <param name="key">The key, e.g., <c>Items[0].Name</c>.</param>
    /// <param name="namingPolicy">The policy that changes each name, or <see langword="null" /> to keep the names.</param>
    /// <returns>The pointer, e.g., <c>/Items/0/Name</c>; empty for the whole input.</returns>
    internal static string ConvertToPointer(string key, JsonNamingPolicy? namingPolicy)
    {
        var segments = TrySplit(key) ?? [key];
        var pointerSegments = new object[segments.Count];
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            pointerSegments[i] = segment is string name && namingPolicy is not null
                ? namingPolicy.ConvertName(name)
                : segment;
        }

        return ErrorDetail.Pointer(pointerSegments);
    }

    #region helper methods

    private static int FindNameEnd(string key, int start)
    {
        var end = key.IndexOfAny([Separator, IndexStart], start);

        return end < 0
            ? key.Length
            : end;
    }

    /// <summary>
    ///     Reads the segment in brackets that starts at <paramref name="start" />: a quoted name or an index.
    /// </summary>
    /// <returns>The index after the segment, or <c>-1</c> if the segment is not valid.</returns>
    private static int ReadBracketSegment(string key, int start, List<object> segments)
    {
        var contentStart = start + 1;
        if (contentStart < key.Length && key[contentStart] == Quote)
        {
            var nameStart = contentStart + 1;
            var nameEnd = key.IndexOf(QuotedNameEnd, nameStart, StringComparison.Ordinal);
            if (nameEnd < 0)
                return -1;

            segments.Add(key[nameStart..nameEnd]);

            return nameEnd + QuotedNameEnd.Length;
        }

        var indexEnd = key.IndexOf(IndexEnd, contentStart);
        if (indexEnd < 0)
            return -1;

        var digits = key.AsSpan(contentStart, indexEnd - contentStart);
        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            return -1;

        segments.Add(index);

        return indexEnd + 1;
    }

    /// <summary>
    ///     Splits the key into its names (<see cref="string" />) and indexes (<see cref="long" />).
    /// </summary>
    /// <returns>The segments, or <see langword="null" /> if the key is not in a known syntax.</returns>
    private static List<object>? TrySplit(string key)
    {
        var segments = new List<object>();
        var position = 0;

        if (key.StartsWith(Root))
        {
            position = 1;
            if (position < key.Length && key[position] is not (Separator or IndexStart))
                return null;
        }
        else
        {
            var nameEnd = FindNameEnd(key, 0);
            if (nameEnd > 0)
                segments.Add(key[..nameEnd]);
            else if (key.Length > 0 && key[0] == Separator)
                return null;

            position = nameEnd;
        }

        while (position < key.Length)
        {
            if (key[position] == Separator)
            {
                var nameStart = position + 1;
                var nameEnd = FindNameEnd(key, nameStart);
                if (nameEnd == nameStart)
                    return null;

                segments.Add(key[nameStart..nameEnd]);
                position = nameEnd;
            }
            else if (key[position] == IndexStart)
            {
                position = ReadBracketSegment(key, position, segments);
                if (position < 0)
                    return null;
            }
            else
            {
                // A name right after a bracket, e.g., Items[0]Name.
                return null;
            }
        }

        return segments;
    }

    #endregion
}
