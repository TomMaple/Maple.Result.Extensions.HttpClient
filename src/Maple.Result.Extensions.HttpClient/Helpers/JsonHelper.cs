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
