using Maple.Json.ObjectAsPrimitiveConverter;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maple.Result.Extensions.HttpClient;

public static class ResultSettingsForHttpClient
{
    internal static JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new ObjectAsPrimitiveConverter(),
            new JsonStringEnumConverter()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static void ConfigureJsonSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        configure(JsonSerializerOptions);
    }
}
