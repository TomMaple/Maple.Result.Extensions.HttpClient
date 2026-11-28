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
using System.Text;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Helpers;

/// <summary>
/// Writes an <see cref="Error" /> as problem details in the format of Maple.Result (docs/problem-details.md of the core), as the
/// packages that write problem details do; the status is the one that the category writes by default.
/// </summary>
internal static class ProblemDetailsWriter
{
    internal static string Write(Error error)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString(ProblemDetailsMembers.Type, error.TypeUri.Value);
            writer.WriteString(ProblemDetailsMembers.Title, error.Title);
            writer.WriteNumber(ProblemDetailsMembers.Status, GetStatus(error.Category));
            if (error.Detail is not null)
                writer.WriteString(ProblemDetailsMembers.Detail, error.Detail);

            if (error.InstanceUri is { } instanceUri)
                writer.WriteString(ProblemDetailsMembers.Instance, instanceUri.Value);

            if (error.ErrorDetails.Count > 0)
            {
                writer.WriteStartArray(ProblemDetailsMembers.Errors);
                foreach (var errorDetail in error.ErrorDetails)
                    WriteErrorDetail(writer, errorDetail);

                writer.WriteEndArray();
            }

            if (error.DetailTemplated is not null)
                WriteTemplatedMessage(writer, ProblemDetailsMembers.DetailTemplated, error.DetailTemplated);

            foreach (var (key, value) in error.Metadata)
            {
                writer.WritePropertyName(key);
                WriteValue(writer, value);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    #region helper methods

    private static int GetStatus(ErrorCategory category)
    {
        return category switch
        {
            ErrorCategory.Validation => 400,
            ErrorCategory.Unauthenticated => 401,
            ErrorCategory.PermissionDenied => 403,
            ErrorCategory.NotFound => 404,
            ErrorCategory.Timeout => 408,
            ErrorCategory.Conflict => 409,
            ErrorCategory.Failure => 422,
            ErrorCategory.LimitExceeded => 429,
            ErrorCategory.Critical => 500,
            ErrorCategory.NotImplemented => 501,
            ErrorCategory.Unavailable => 503,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
    }

    private static void WriteErrorDetail(Utf8JsonWriter writer, ErrorDetail errorDetail)
    {
        writer.WriteStartObject();
        if (errorDetail.PropertyPointer is not null)
            writer.WriteString(ProblemDetailsMembers.ErrorPointer, errorDetail.PropertyPointer);

        writer.WriteString(ProblemDetailsMembers.ErrorDetail, errorDetail.Detail);
        if (errorDetail.DetailTemplated is not null)
            WriteTemplatedMessage(writer, ProblemDetailsMembers.ErrorDetailTemplated, errorDetail.DetailTemplated);

        writer.WriteEndObject();
    }

    private static void WriteTemplatedMessage(Utf8JsonWriter writer, string propertyName, TemplatedMessage templatedMessage)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteString(ProblemDetailsMembers.TemplateId, templatedMessage.TemplateId);
        if (templatedMessage.Parameters is not null)
        {
            writer.WriteStartObject(ProblemDetailsMembers.TemplateParams);
            foreach (var (name, value) in templatedMessage.Parameters)
            {
                writer.WritePropertyName(name);
                WriteValue(writer, value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;

            case string text:
                writer.WriteStringValue(text);
                break;

            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;

            case int number:
                writer.WriteNumberValue(number);
                break;

            case long number:
                writer.WriteNumberValue(number);
                break;

            case decimal number:
                writer.WriteNumberValue(number);
                break;

            case double number:
                writer.WriteNumberValue(number);
                break;

            default:
                throw new NotSupportedException($"A value of the type ‘{value.GetType()}’ is not written by this helper.");
        }
    }

    #endregion
}
