using System.Collections.Generic;
using System.Linq;
using Maple.Result.Extensions.HttpClient.Extensions;
using Maple.Result.Extensions.HttpClient.Helpers;
using Maple.Result.Extensions.HttpClient.InternalModels;
using System.Text.Json;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class ErrorDetailsMapper
{
    #region consts

    private const string DetailPropertyName = "detail";
    private const string PropertyPointerPropertyName = "pointer";
    private const string DetailTemplatedPropertyName = "detailTemplated";

    #endregion

    internal static IReadOnlyList<ErrorDetail>? TryMap(object? source)
    {
        var errorDetails = source switch
        {
            object[] array => TryMapErrorsArray(array),
            IDictionary<string, object?> dictionary => TryMapErrorsDictionary(dictionary),
            JsonElement { ValueKind: JsonValueKind.Array } jsonArray => TryMapErrorsJsonArray(jsonArray),
            JsonElement { ValueKind: JsonValueKind.Object } jsonObject => TryMapErrorsJsonObject(jsonObject),
            _ => null
        };

        return errorDetails is { Count: > 0 }
            ? errorDetails
            : null;
    }

    private static IReadOnlyList<ErrorDetail> TryMapErrorsArray(object[] sourceArray)
    {
        var errorDetails = new List<ErrorDetail>();
        foreach (var sourceItem in sourceArray)
        {
            if (sourceItem is not IDictionary<string, object?> itemDictionary)
                continue;

            var errorDetail = TryMapItem(itemDictionary);
            if (errorDetail is not null)
                errorDetails.Add(errorDetail);
        }

        return errorDetails;
    }

    private static IReadOnlyList<ErrorDetail> TryMapErrorsDictionary(IDictionary<string, object?> sourceDictionary)
    {
        var errorDetails = sourceDictionary
            .SelectMany(kv => TryMapItem(kv))
            .ToArray();

        return errorDetails;
    }

    private static IReadOnlyList<ErrorDetail>? TryMapErrorsJsonArray(JsonElement jsonArray)
    {
        var mappedArray = JsonHelper.TryDeserialize<IReadOnlyList<ErrorDetailInternal>>(jsonArray);

        var errorDetails = mappedArray
            ?.Select(TryMap)
            .OfType<ErrorDetail>()
            .ToArray();

        return errorDetails;
    }

    private static IReadOnlyList<ErrorDetail>? TryMapErrorsJsonObject(JsonElement jsonObject)
    {
        var mappedDictionary = JsonHelper.TryDeserialize<Dictionary<string, object?>>(jsonObject);

        var errorDetails = mappedDictionary
            ?.SelectMany(kv => TryMapItem(kv))
            .ToArray();

        return errorDetails;
    }

    private static IReadOnlyList<ErrorDetail> TryMapItem(KeyValuePair<string, object?>? source)
    {
        if (source is null)
            return [];

        var pair = source.Value;
        if (pair.Value is not string[] values || string.IsNullOrWhiteSpace(pair.Key))
            return [];

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(v => new ErrorDetail(pair.Key, v))
            .ToArray();
    }

    private static ErrorDetail? TryMapItem(IDictionary<string, object?> source)
    {
        var detailValue = source.GetValueOrNull(DetailPropertyName);
        if (detailValue is not string detail || string.IsNullOrWhiteSpace(detail))
            return null;

        var propertyPointerValue = source.GetValueOrNull(PropertyPointerPropertyName);
        var propertyPointer = propertyPointerValue as string;

        var detailTemplatedValue = source.GetValueOrNull(DetailTemplatedPropertyName);
        var detailTemplated = TemplatedMessageMapper.TryMap(detailTemplatedValue);

        return new ErrorDetail(propertyPointer, detail, detailTemplated);
    }

    private static ErrorDetail? TryMap(ErrorDetailInternal? source)
    {
        if (source == null) 
            return null;

        if (string.IsNullOrWhiteSpace(source.Detail))
            return null;

        var detailTemplated = TemplatedMessageMapper.TryMap(source.DetailTemplated);
        return new ErrorDetail(source.PropertyPointer, source.Detail, detailTemplated);
    }
}
