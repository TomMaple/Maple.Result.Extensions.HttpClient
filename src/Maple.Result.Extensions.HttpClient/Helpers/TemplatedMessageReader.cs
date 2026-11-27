// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Results.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Reads a <c>detailTemplated</c> object of problem details, of the error or of an item of <c>errors</c>.
/// </summary>
internal static class TemplatedMessageReader
{
    /// <summary>
    ///     Returns the templated message of the element: its <c>templateId</c> and its <c>params</c>, whose values are read by
    ///     <see cref="JsonValueConverter" />.
    /// </summary>
    /// <returns>
    ///     The message, or <see langword="null" /> if the element is not an object, the identifier is not a string, <c>params</c> is
    ///     neither an object nor <c>null</c>, or the core rejects the message (e.g., a blank identifier, or parameter names that differ
    ///     only in case).
    /// </returns>
    internal static TemplatedMessage? Read(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        JsonElement? templateIdElement = null;
        JsonElement? parametersElement = null;
        foreach (var property in element.EnumerateObject())
        {
            switch (property.Name)
            {
                case ProblemDetailsMembers.TemplateId:
                    templateIdElement = property.Value;
                    break;
                case ProblemDetailsMembers.TemplateParams:
                    parametersElement = property.Value;
                    break;
            }
        }

        if (templateIdElement is not { ValueKind: JsonValueKind.String } templateIdValue)
            return null;

        var templateId = templateIdValue.GetString()!;
        List<KeyValuePair<string, object?>>? parameters = null;
        if (parametersElement is { ValueKind: JsonValueKind.Object } parametersObject)
            parameters = ReadParameters(parametersObject);
        else if (parametersElement is { ValueKind: not JsonValueKind.Null })
            return null;

        return CreateMessage(templateId, parameters);
    }

    #region helper methods

    private static TemplatedMessage? CreateMessage(string templateId, List<KeyValuePair<string, object?>>? parameters)
    {
        try
        {
            return new TemplatedMessage(templateId, parameters);
        }
        catch (ArgumentException)
        {
            // The core rejects the identifier or a parameter name; the message is dropped as a whole.
            return null;
        }
    }

    private static List<KeyValuePair<string, object?>> ReadParameters(JsonElement element)
    {
        var parameters = new List<KeyValuePair<string, object?>>();
        foreach (var property in element.EnumerateObject())
        {
            var value = JsonValueConverter.ConvertToValue(property.Value);
            parameters.Add(new KeyValuePair<string, object?>(property.Name, value));
        }

        return parameters;
    }

    #endregion
}
