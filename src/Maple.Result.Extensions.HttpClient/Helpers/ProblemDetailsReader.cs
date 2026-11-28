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
using System.Net.Http;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Reads a problem details body (<i>RFC 9457</i>) into the builder of an error, as <i>Reading problem details</i> of
///     <c>docs/problem-details.md</c> of the core describes.
/// </summary>
/// <remarks>
///     <para>
///         The body is parsed into a <see cref="JsonDocument" />, without reflection, so the reader works in a trimmed or Native AOT
///         application. The member names are the constants of <see cref="ProblemDetailsMembers" /> and are matched exactly; when a member
///         is repeated, the last one is read.
///     </para>
///     <para>
///         The builder has the error of the status code; each member that is read replaces a part of it, and a member that cannot be read
///         leaves that part as it is. A body that is not a <i>JSON</i> object leaves the builder unchanged. Nothing throws, apart from an
///         exception of the naming policy of the caller.
///     </para>
/// </remarks>
internal static class ProblemDetailsReader
{
    #region consts

    private const string JsonMediaType = "application/json";
    private const string JsonMediaTypeSuffix = "+json";

    // The characters of a URI besides letters, digits, % and # (RFC 3986, section 2): unreserved, sub-delims, ":", "@", "/" and "?".
    private const string UriCharacters = "-._~!$&'()*+,;=:@/?";

    #endregion

    /// <summary>
    ///     Whether the body is read as problem details: its media type is <c>application/json</c> or has the <c>+json</c> suffix (e.g.,
    ///     <c>application/problem+json</c>), or the response has no <c>Content-Type</c> and the body starts with <c>{</c>.
    /// </summary>
    /// <param name="content">The content of the response, for its <c>Content-Type</c> header.</param>
    /// <param name="text">The body as text.</param>
    internal static bool CanRead(HttpContent content, string text)
    {
        var mediaType = content.Headers.ContentType?.MediaType;
        if (mediaType is null)
            return text.AsSpan().TrimStart(" \t\r\n").StartsWith('{');

        return mediaType.Equals(JsonMediaType, StringComparison.OrdinalIgnoreCase)
               || mediaType.EndsWith(JsonMediaTypeSuffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Reads the members of the body into the builder.
    /// </summary>
    /// <param name="text">The body as text.</param>
    /// <param name="builder">The builder with the error of the status code.</param>
    /// <param name="requestUri">The URI of the request, against which a relative <c>type</c> or <c>instance</c> is resolved.</param>
    /// <param name="errorKeyNamingPolicy">The policy that changes the names in the keys of the <c>errors</c> object of ASP.NET Core.</param>
    internal static void Read(string text, ErrorBuilder builder, Uri? requestUri, JsonNamingPolicy? errorKeyNamingPolicy)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            // Not JSON (or nested deeper than the reader allows): the error of the status code stays.
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
                ReadMembers(root, builder, requestUri, errorKeyNamingPolicy);
        }
    }

    #region helper methods

    /// <summary>
    ///     Adds an extension member to the metadata, under its own name; a key that the core rejects (e.g., blank, with white space at the
    ///     ends, or a reserved member in another case such as <c>Title</c>) is dropped.
    /// </summary>
    private static void AddMetadata(ErrorBuilder builder, JsonProperty member)
    {
        var value = JsonValueConverter.ConvertToValue(member.Value);
        try
        {
            builder.WithMetadata(member.Name, value);
        }
        catch (ArgumentException)
        {
            // The core rejects the key; the member is dropped.
        }
    }

    private static string? GetText(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.String } value)
            return null;

        var text = value.GetString();

        return string.IsNullOrWhiteSpace(text)
            ? null
            : text;
    }

    /// <summary>
    ///     Whether the text is a relative-path reference (<i>RFC 3986</i>, section 4.2), e.g., <c>out-of-credit</c>, <c>../probs/x</c> or
    ///     <c>#x</c>: not a reference with a scheme, not one that starts with <c>/</c>, and only with the characters of a URI.
    /// </summary>
    /// <remarks>
    ///     <see cref="Uri.IsWellFormedUriString" /> is not used: it rejects a reference with only a fragment (<c>#x</c>), and
    ///     <see cref="Uri.TryCreate(Uri, string, out Uri)" /> escapes what is not allowed (e.g., a space) instead of rejecting it.
    /// </remarks>
    private static bool IsRelativePathReference(string text)
    {
        if (text.StartsWith('/'))
            return false;

        // A colon before the first /, ? or # makes a scheme (RFC 3986, section 4.2), e.g., http: or tag:.
        var end = text.AsSpan().IndexOfAny(":/?#");
        if (end >= 0 && text[end] == ':')
            return false;

        var hasFragment = false;
        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (character == '%')
            {
                // A percent-encoded octet: % and two hexadecimal digits.
                if (i + 2 >= text.Length || !char.IsAsciiHexDigit(text[i + 1]) || !char.IsAsciiHexDigit(text[i + 2]))
                    return false;

                i += 2;
            }
            else if (character == '#')
            {
                if (hasFragment)
                    return false;

                hasFragment = true;
            }
            else if (!char.IsAsciiLetterOrDigit(character) && !UriCharacters.Contains(character))
            {
                return false;
            }
        }

        return true;
    }

    private static void ReadMembers(JsonElement root, ErrorBuilder builder, Uri? requestUri, JsonNamingPolicy? errorKeyNamingPolicy)
    {
        JsonElement? typeElement = null;
        JsonElement? titleElement = null;
        JsonElement? detailElement = null;
        JsonElement? instanceElement = null;
        JsonElement? errorsElement = null;
        JsonElement? detailTemplatedElement = null;

        foreach (var member in root.EnumerateObject())
        {
            switch (member.Name)
            {
                case ProblemDetailsMembers.Type:
                    typeElement = member.Value;
                    break;

                case ProblemDetailsMembers.Title:
                    titleElement = member.Value;
                    break;

                case ProblemDetailsMembers.Status:
                    // The status code of the response decides the category; the status member is advisory (RFC 9457, section 3.1.2).
                    break;

                case ProblemDetailsMembers.Detail:
                    detailElement = member.Value;
                    break;

                case ProblemDetailsMembers.Instance:
                    instanceElement = member.Value;
                    break;

                case ProblemDetailsMembers.Errors:
                    errorsElement = member.Value;
                    break;

                case ProblemDetailsMembers.DetailTemplated:
                    detailTemplatedElement = member.Value;
                    break;

                default:
                    AddMetadata(builder, member);
                    break;
            }
        }

        var typeUri = ReadUri(typeElement, requestUri);
        if (typeUri is not null)
            builder.WithTypeUri(typeUri.Value);

        var title = GetText(titleElement);
        if (title is not null)
            builder.WithTitle(title);

        var detail = GetText(detailElement);
        if (detail is not null)
            builder.WithDetail(detail);

        var instanceUri = ReadUri(instanceElement, requestUri);
        if (instanceUri is not null)
            builder.WithInstanceUri(instanceUri);

        if (errorsElement is { } errors)
            ErrorDetailsReader.Read(errors, builder, errorKeyNamingPolicy);

        var detailTemplated = detailTemplatedElement is { } detailTemplatedValue
            ? TemplatedMessageReader.Read(detailTemplatedValue)
            : null;

        if (detailTemplated is not null)
            builder.WithDetailTemplated(detailTemplated);
    }

    /// <summary>
    ///     Reads a <c>type</c> or an <c>instance</c> with <see cref="ErrorUri.TryParse" />; a relative-path reference (e.g.,
    ///     <c>out-of-credit</c>) is resolved against the URI of the request (<i>RFC 3986</i>, section 5).
    /// </summary>
    /// <returns>The URI, or <see langword="null" /> if the member is missing, blank, not a string or not a URI reference.</returns>
    private static ErrorUri? ReadUri(JsonElement? element, Uri? requestUri)
    {
        var text = GetText(element);
        if (text is null)
            return null;

        if (ErrorUri.TryParse(text, out var uri))
            return uri;

        if (requestUri is not { IsAbsoluteUri: true } || !IsRelativePathReference(text))
            return null;

        if (!Uri.TryCreate(requestUri, text, out var resolvedUri))
            return null;

        return ErrorUri.TryParse(resolvedUri.AbsoluteUri, out var resolvedErrorUri)
            ? resolvedErrorUri
            : null;
    }

    #endregion
}
