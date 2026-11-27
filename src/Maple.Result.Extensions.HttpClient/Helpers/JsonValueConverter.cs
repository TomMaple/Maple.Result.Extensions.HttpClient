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
///     Turns a <i>JSON</i> value into a plain .NET value, for the metadata and the template parameters of an error.
/// </summary>
/// <remarks>
///     A <see cref="JsonElement" /> is not stored: <c>TryGetMetadata&lt;T&gt;</c> of the core does not convert, the serializer packages
///     cannot write it, and it is not valid after its document is disposed.
/// </remarks>
internal static class JsonValueConverter
{
    /// <summary>
    ///     Returns the value of the element.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" />, a <see cref="bool" />, <see langword="null" />, a number (see <see cref="ConvertNumber" />), an
    ///     <see cref="object" /> array for an array, or a <see cref="Dictionary{TKey,TValue}" /> for an object (with the same rules inside,
    ///     and the last value of a name that is repeated).
    /// </returns>
    internal static object? ConvertToValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => ConvertNumber(element),
            JsonValueKind.Array => ConvertArray(element),
            JsonValueKind.Object => ConvertObject(element),
            _ => null
        };
    }

    #region helper methods

    private static object?[] ConvertArray(JsonElement element)
    {
        var values = new object?[element.GetArrayLength()];
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            values[index] = ConvertToValue(item);
            index++;
        }

        return values;
    }

    /// <summary>
    ///     Returns the number: an integer as an <see cref="int" />, else a <see cref="long" />, else a <see cref="decimal" />; a number with a
    ///     fraction or an exponent as a <see cref="decimal" />; else a <see cref="double" /> (with the precision of a double); else, beyond
    ///     the range of a double, its text.
    /// </summary>
    /// <remarks>
    ///     A <see cref="decimal" /> keeps the digits that a <see cref="double" /> would round (e.g., <c>0.1</c>). A number that a decimal
    ///     rounds to zero (e.g., <c>1e-30</c>) is a double, and infinity is never returned, as <i>JSON</i> cannot write it.
    /// </remarks>
    private static object ConvertNumber(JsonElement element)
    {
        var text = element.GetRawText();
        var isInteger = text.AsSpan().IndexOfAny('.', 'e', 'E') < 0;
        if (isInteger)
        {
            if (element.TryGetInt32(out var intValue))
                return intValue;

            if (element.TryGetInt64(out var longValue))
                return longValue;
        }

        // A decimal that is zero for a number that is not (all digits of its mantissa are 0) lost the number.
        var exponentStart = text.AsSpan().IndexOfAny('e', 'E');
        var mantissa = exponentStart < 0
            ? text.AsSpan()
            : text.AsSpan(0, exponentStart);

        var isZero = !mantissa.ContainsAnyInRange('1', '9');
        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue) && (decimalValue != 0 || isZero))
            return decimalValue;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue) && double.IsFinite(doubleValue))
            return doubleValue;

        return text;
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement element)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            values[property.Name] = ConvertToValue(property.Value);

        return values;
    }

    #endregion
}
