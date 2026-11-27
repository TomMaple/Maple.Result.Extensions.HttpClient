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
using System.Globalization;
using System.Net;

namespace Maple.Results.Extensions.HttpClient;

/// <summary>
///     Extension methods of <see cref="Error" /> that read what this package stores about the HTTP response of an error.
/// </summary>
public static class ErrorHttpExtensions
{
    #region consts

    // The range of the status codes that an HttpResponseMessage can have.
    private const int MaxStatusCode = 999;
    private const int MinStatusCode = 0;

    #endregion

    /// <summary>
    ///     Gets the HTTP status code of the response that the error was mapped from, stored under
    ///     <see cref="HttpErrorMetadataKeys.HttpStatusCode" />.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <param name="statusCode">The status code if the error has one; otherwise, the default value (<c>0</c>).</param>
    /// <returns><see langword="true" /> if the error has a status code; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     <para>
    ///         The key is found whatever its case. The value is written as an <see cref="int" />; because a number comes back from JSON in
    ///         whatever type the deserializer chose, any integral type and a <see cref="decimal" />, <see cref="double" /> or
    ///         <see cref="float" /> without a fraction are read as well. The number has to be from 0 to 999, the range of a
    ///         <see cref="System.Net.Http.HttpResponseMessage" />; any other value (e.g., a text) counts as absent.
    ///     </para>
    ///     <para>An error that was not mapped from a response (e.g., one created by the application) has no status code.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">If the <paramref name="error" /> is <see langword="null" />.</exception>
    public static bool TryGetHttpStatus(this Error error, out HttpStatusCode statusCode)
    {
        ArgumentNullException.ThrowIfNull(error);

        statusCode = default;

        if (!error.Metadata.TryGetValue(HttpErrorMetadataKeys.HttpStatusCode, out var value))
            return false;

        if (!TryGetInteger(value, out var number))
            return false;

        var isStatusCode = number is >= MinStatusCode and <= MaxStatusCode;
        if (!isStatusCode)
            return false;

        statusCode = (HttpStatusCode)number;

        return true;
    }

    #region helper methods

    /// <summary>
    ///     Reads a number of any type that is a whole number in the range of an <see cref="int" />.
    /// </summary>
    /// <remarks>
    ///     A <see cref="decimal" /> is checked in its own type: a conversion to a <see cref="double" /> is not exact, so it would turn a
    ///     whole number with trailing zeros (e.g., <c>404.00000000000000000000</c>) into a fraction and drop a small fraction.
    /// </remarks>
    private static bool TryGetInteger(object? value, out int number)
    {
        switch (value)
        {
            case int intValue:
                number = intValue;
                return true;

            case short or sbyte or byte or ushort:
                number = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return true;

            case decimal decimalValue when decimal.IsInteger(decimalValue) && decimalValue is >= int.MinValue and <= int.MaxValue:
                number = (int)decimalValue;
                return true;

            // Every whole number in the range of an int converts to a double exactly.
            case long or uint or ulong or double or float:
                var doubleValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                var isInteger = double.IsInteger(doubleValue) && doubleValue is >= int.MinValue and <= int.MaxValue;
                number = isInteger
                    ? (int)doubleValue
                    : 0;

                return isInteger;

            default:
                number = 0;
                return false;
        }
    }

    #endregion
}
