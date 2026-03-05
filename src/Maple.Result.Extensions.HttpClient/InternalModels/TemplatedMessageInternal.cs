using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Maple.Result.Extensions.HttpClient.InternalModels;

internal record TemplatedMessageInternal
{
    internal const string TemplateIdPropertyName = "messageId";
    internal const string ParamsPropertyName = "params";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName(TemplateIdPropertyName)]
    public string TemplateId { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName(ParamsPropertyName)]
    public IReadOnlyDictionary<string, object?>? Params { get; init; }
}
