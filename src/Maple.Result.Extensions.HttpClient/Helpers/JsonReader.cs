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
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Reads the types of the caller through the metadata of the <see cref="JsonSerializerOptions" />, without reflection of its own, so
///     that the package works in a trimmed or Native AOT application.
/// </summary>
internal static class JsonReader
{
    /// <summary>
    ///     Returns the metadata of <typeparamref name="T" /> that the options resolve.
    /// </summary>
    /// <exception cref="NotSupportedException">
    ///     If the options have no metadata of <typeparamref name="T" /> (e.g., a source-generated context that does not name the type).
    /// </exception>
    internal static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions jsonSerializerOptions)
    {
        var type = typeof(T);
        var jsonTypeInfo = jsonSerializerOptions.GetTypeInfo(type);

        return (JsonTypeInfo<T>)jsonTypeInfo;
    }

    /// <summary>
    ///     Reads the text as a <typeparamref name="T" />; the default value when the text is blank, not <i>JSON</i> or not a
    ///     <typeparamref name="T" />.
    /// </summary>
    /// <exception cref="NotSupportedException">
    ///     If the text is not blank and the options have no metadata of <typeparamref name="T" />.
    /// </exception>
    internal static T? ReadOrDefault<T>(string text, JsonSerializerOptions jsonSerializerOptions)
    {
        if (string.IsNullOrWhiteSpace(text))
            return default;

        var jsonTypeInfo = GetTypeInfo<T>(jsonSerializerOptions);

        try
        {
            return JsonSerializer.Deserialize(text, jsonTypeInfo);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
