using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Maple.Result.Extensions.HttpClient.Converters;

namespace Maple.Result.Extensions.HttpClient.Helpers;

internal static class JsonHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,  // TODO: do we need that?
        Converters = { new ObjectAsPrimitiveConverter() }
    };

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

    internal static T? TryDeserialize<T>(JsonDocument jsonDocument)
    {
        try
        {
            //return jsonDocument.RootElement.Deserialize<T>(JsonSerializerOptions);
            return jsonDocument.Deserialize<T>(JsonSerializerOptions);
        }
        catch
        {
            return default;
        }
    }

    internal static T? TryDeserialize<T>(JsonElement jsonElement)
    {
        try
        {
            return jsonElement.Deserialize<T>(JsonSerializerOptions);
        }
        catch
        {
            return default;
        }
    }

    internal static T? TryDeserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        }
        catch
        {
            return default;
        }
    }
}
