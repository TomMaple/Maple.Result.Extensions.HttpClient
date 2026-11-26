// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     Reads the content of a response as text or as <i>JSON</i>, in the character set that its <c>Content-Type</c> header names.
/// </summary>
/// <remarks>
///     A character set that the runtime does not know (e.g., <c>windows-1252</c> without its provider, or the misspelt <c>utf8</c>) is
///     read as UTF-8. <see cref="HttpContent.ReadAsStringAsync(CancellationToken)" /> throws an <see cref="InvalidOperationException" />
///     for it, which would turn the error page of a proxy into an exception.
/// </remarks>
internal static class HttpContentReader
{
    #region consts

    private const string TextMediaTypePrefix = "text/";

    #endregion

    /// <summary>
    ///     Whether the content is known to be empty without reading it: its length is zero.
    /// </summary>
    /// <remarks>The length of a content that is streamed (e.g., a chunked response) is not known; such a content is not empty here.</remarks>
    internal static bool IsEmpty(HttpContent content)
    {
        return content.Headers.ContentLength == 0;
    }

    /// <summary>
    ///     Whether the media type of the content is a text one (<c>text/*</c>).
    /// </summary>
    internal static bool IsText(HttpContent content)
    {
        var mediaType = content.Headers.ContentType?.MediaType;

        return mediaType is not null && mediaType.StartsWith(TextMediaTypePrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Reads the content as <i>JSON</i> from its stream, which is disposed afterwards.
    /// </summary>
    /// <returns>The value, or <see langword="null" /> for the <i>JSON</i> value <c>null</c>.</returns>
    /// <exception cref="JsonException">If the content is not <i>JSON</i> or not a <typeparamref name="T" />.</exception>
    internal static async Task<T?> ReadJsonAsync<T>(HttpContent content, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
    {
        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var utf8Stream = ToUtf8(stream, content);

        await using (utf8Stream.ConfigureAwait(false))
        {
            return await JsonSerializer.DeserializeAsync(utf8Stream, jsonTypeInfo, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Reads the content as text; an empty text when there is no content.
    /// </summary>
    internal static async Task<string> ReadTextAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var charSet = GetCharSet(content);
        var hasUnknownCharSet = charSet is not null && FindEncoding(charSet) is null;
        if (!hasUnknownCharSet)
            return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var bytes = await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        return DecodeUtf8(bytes);
    }

    #region helper methods

    private static string DecodeUtf8(byte[] bytes)
    {
        var text = bytes.AsSpan();
        var byteOrderMark = Encoding.UTF8.Preamble;
        if (text.StartsWith(byteOrderMark))
            text = text[byteOrderMark.Length..];

        return Encoding.UTF8.GetString(text);
    }

    /// <summary>
    ///     Returns the encoding of the character set, or <see langword="null" /> when the runtime does not know it.
    /// </summary>
    private static Encoding? FindEncoding(string charSet)
    {
        try
        {
            return Encoding.GetEncoding(charSet);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Returns the character set that the <c>Content-Type</c> header names, or <see langword="null" /> when it names none.
    /// </summary>
    private static string? GetCharSet(HttpContent content)
    {
        var charSet = content.Headers.ContentType?.CharSet;
        if (string.IsNullOrWhiteSpace(charSet))
            return null;

        // As HttpContent does: at most one pair of quotes is removed.
        var isQuoted = charSet.Length > 2 && charSet[0] == '"' && charSet[^1] == '"';

        return isQuoted
            ? charSet[1..^1]
            : charSet;
    }

    /// <summary>
    ///     Returns a stream that gives the content in UTF-8, which the <see cref="JsonSerializer" /> reads.
    /// </summary>
    private static Stream ToUtf8(Stream stream, HttpContent content)
    {
        var charSet = GetCharSet(content);
        if (charSet is null)
            return stream;

        // JSON is UTF-8 unless stated otherwise (RFC 8259, section 8.1), so an unknown character set is read as UTF-8 too.
        var encoding = FindEncoding(charSet);
        if (encoding is null || encoding.CodePage == Encoding.UTF8.CodePage)
            return stream;

        return Encoding.CreateTranscodingStream(stream, encoding, Encoding.UTF8);
    }

    #endregion
}
