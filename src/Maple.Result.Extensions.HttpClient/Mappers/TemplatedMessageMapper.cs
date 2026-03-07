using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Maple.Result.Extensions.HttpClient.Extensions;
using Maple.Result.Extensions.HttpClient.Helpers;
using Maple.Result.Extensions.HttpClient.InternalModels;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class TemplatedMessageMapper
{
    internal static TemplatedMessage? TryMap(object? source)
    {
        return source switch
        {
            IDictionary<string, object?> dictionary => TryMap(dictionary),
            JsonElement jsonElement => TryMap(jsonElement),
            _ => null
        };
    }

    private static TemplatedMessage? TryMap(IDictionary<string, object?> dictionary)
    {
        var templateIdObject = dictionary.GetValueOrNull(TemplatedMessageInternal.TemplateIdPropertyName);
        if (templateIdObject is not string templateId || string.IsNullOrWhiteSpace(templateId))
            return null;

        var paramsValue = dictionary.GetValueOrNull(TemplatedMessageInternal.ParamsPropertyName);
        var validParams = GetValidParamsOrNull(paramsValue);

        return new TemplatedMessage(templateId, validParams);
    }

    private static TemplatedMessage? TryMap(JsonElement jsonElement)
    {
        if (jsonElement is { ValueKind: JsonValueKind.Object })
        {
            var templatedMessageInternal = JsonHelper.TryDeserialize<TemplatedMessageInternal>(jsonElement);
            return TryMap(templatedMessageInternal);
        }

        return null;
    }

    private static TemplatedMessage? TryMap(TemplatedMessageInternal? source)
    {
        if (source is null || string.IsNullOrWhiteSpace(source.TemplateId))
            return null;

        var validParams = source.Params
            ?.Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value!);

        if (validParams?.Count == 0)
            validParams = null;

        return new TemplatedMessage(source.TemplateId, validParams);
    }

    private static IReadOnlyDictionary<string, object>? GetValidParamsOrNull(object? source)
    {
        if (source is not IDictionary<string, object?> dictionary)
            return null;

        var validParams = dictionary
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value!);

        if (validParams is { Count: > 0 })
            return validParams;

        return null;
    }
}
