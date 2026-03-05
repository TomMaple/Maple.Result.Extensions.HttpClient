using System.Text.Json.Serialization;

namespace Maple.Result.Extensions.HttpClient.InternalModels;

internal record ErrorDetailInternal
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("pointer")]
    public string? PropertyPointer { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("detail")]
    public string Detail { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("detailTemplated")]
    public TemplatedMessageInternal? DetailTemplated { get; init; }
}
