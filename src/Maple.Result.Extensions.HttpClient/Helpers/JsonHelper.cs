// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Maple.Result.Extensions.HttpClient.Converters;

namespace Maple.Result.Extensions.HttpClient.Helpers;

internal static class JsonHelper
{
    internal static bool IsValidJson(string text, [NotNullWhen(true)] out JsonDocument? jsonDoc)
    {
        if (string.IsNullOrEmpty(text))
        {
            jsonDoc = null;
            return false;
        }

        try
        {
            jsonDoc = JsonDocument.Parse(text);
            return true;
        }
        catch
        {
            jsonDoc = null;
            return false;
        }
    }

    internal static bool TryDeserialize<T>(JsonDocument jsonDocument, [NotNullWhen(true)] out T? value, JsonSerializerOptions? customOptions = null)
    {
        var options = GetJsonSerializerOptions(customOptions);

        try
        {
            //return jsonDocument.RootElement.Deserialize<T>(JsonSerializerOptions);
            value = jsonDocument.Deserialize<T>(options);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    internal static bool TryDeserialize<T>(JsonElement jsonElement, [NotNullWhen(true)] out T? value, JsonSerializerOptions? customOptions = null)
    {
        var options = GetJsonSerializerOptions(customOptions);

        try
        {
            value = jsonElement.Deserialize<T>(options);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    internal static bool TryDeserialize<T>(string json, [NotNullWhen(true)] out T? value, JsonSerializerOptions? customOptions = null)
    {
        var options = GetJsonSerializerOptions(customOptions);

        if (string.IsNullOrWhiteSpace(json))
        {
            value = default;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T?>(json, options);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    #region helper methods

    private static JsonSerializerOptions GetJsonSerializerOptions(JsonSerializerOptions? customOptions)
    {
        if (customOptions is null)
            return ResultSettingsForHttpClient.JsonSerializerOptions;

        if (customOptions.Converters.Count == 0
            || customOptions.Converters.All(x => x.GetType() != typeof(ObjectAsPrimitiveConverter)))
        {
            customOptions.Converters.Add(new ObjectAsPrimitiveConverter());
        }

        return customOptions;
    }

    #endregion
}
