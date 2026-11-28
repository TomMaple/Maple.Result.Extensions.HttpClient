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
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Reads the <c>errors</c> member of problem details into the error details of a builder.
/// </summary>
/// <remarks>
///     Two shapes are read: the array of <i>RFC 9457</i> and of the core (items with <c>pointer</c>, <c>detail</c> and
///     <c>detailTemplated</c>), and the object of ASP.NET Core (<c>ValidationProblemDetails</c>, a key with its messages, e.g.,
///     <c>{"Email": ["…"]}</c>). Any other value adds nothing.
/// </remarks>
internal static class ErrorDetailsReader
{
    /// <summary>
    ///     Adds the error details of the element to the builder.
    /// </summary>
    /// <param name="element">The value of the <c>errors</c> member.</param>
    /// <param name="builder">The builder of the error.</param>
    /// <param name="errorKeyNamingPolicy">The policy that changes the names in the keys of the object of ASP.NET Core.</param>
    internal static void Read(JsonElement element, ErrorBuilder builder, JsonNamingPolicy? errorKeyNamingPolicy)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                ReadArray(element, builder);
                break;

            case JsonValueKind.Object:
                ReadObject(element, builder, errorKeyNamingPolicy);
                break;
        }
    }

    #region helper methods

    private static void AddMessage(ErrorBuilder builder, string pointer, JsonElement element)
    {
        var message = GetMessage(element);
        if (message is not null)
            builder.AddErrorDetail(new ErrorDetail(pointer, message));
    }

    /// <summary>
    ///     Creates the error detail; an invalid pointer is dropped and the detail is kept (<i>RFC 6901</i>, section 7).
    /// </summary>
    private static ErrorDetail CreateErrorDetail(string? pointer, string detail, TemplatedMessage? detailTemplated)
    {
        if (pointer is null)
            return new ErrorDetail(null, detail, detailTemplated);

        try
        {
            return new ErrorDetail(pointer, detail, detailTemplated);
        }
        catch (ArgumentException)
        {
            // The detail is not blank (see ReadItem), so the core rejected the pointer.
            return new ErrorDetail(null, detail, detailTemplated);
        }
    }

    private static string? GetMessage(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
            return null;

        var message = element.GetString();

        return string.IsNullOrWhiteSpace(message)
            ? null
            : message;
    }

    private static void ReadArray(JsonElement element, ErrorBuilder builder)
    {
        foreach (var item in element.EnumerateArray())
        {
            var errorDetail = ReadItem(item);
            if (errorDetail is not null)
                builder.AddErrorDetail(errorDetail);
        }
    }

    /// <summary>
    ///     Reads an item of the array; one without a detail that the core accepts is dropped.
    /// </summary>
    private static ErrorDetail? ReadItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return null;

        JsonElement? pointerElement = null;
        JsonElement? detailElement = null;
        JsonElement? detailTemplatedElement = null;
        foreach (var property in item.EnumerateObject())
        {
            switch (property.Name)
            {
                case ProblemDetailsMembers.ErrorPointer:
                    pointerElement = property.Value;
                    break;

                case ProblemDetailsMembers.ErrorDetail:
                    detailElement = property.Value;
                    break;

                case ProblemDetailsMembers.ErrorDetailTemplated:
                    detailTemplatedElement = property.Value;
                    break;
            }
        }

        var detail = detailElement is { } detailValue
            ? GetMessage(detailValue)
            : null;

        if (detail is null)
            return null;

        var pointer = pointerElement is { ValueKind: JsonValueKind.String } pointerValue
            ? pointerValue.GetString()
            : null;

        var detailTemplated = detailTemplatedElement is { } detailTemplatedValue
            ? TemplatedMessageReader.Read(detailTemplatedValue)
            : null;

        return CreateErrorDetail(pointer, detail, detailTemplated);
    }

    /// <summary>
    ///     Reads the object of ASP.NET Core: one error detail per message of a key, with the pointer of the key. A single message instead
    ///     of an array is read too; a value that is not a message is dropped.
    /// </summary>
    private static void ReadObject(JsonElement element, ErrorBuilder builder, JsonNamingPolicy? errorKeyNamingPolicy)
    {
        foreach (var property in element.EnumerateObject())
        {
            var messages = property.Value;
            if (messages.ValueKind is not (JsonValueKind.Array or JsonValueKind.String))
                continue;

            var pointer = ErrorKeyConverter.ConvertToPointer(property.Name, errorKeyNamingPolicy);
            if (messages.ValueKind == JsonValueKind.String)
            {
                AddMessage(builder, pointer, messages);
                continue;
            }

            foreach (var message in messages.EnumerateArray())
                AddMessage(builder, pointer, message);
        }
    }

    #endregion
}
