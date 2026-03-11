// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

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
