// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Maple.Results.Extensions.HttpClient.Tests.Unit;

/// <summary>
/// The types are part of the wire format: callers and other services recognise an error by them, so a change is a breaking change.
/// </summary>
public class HttpClientErrorUrisUnitTests
{
    public static TheoryData<string, ErrorUri, string> TypeUris
        => new()
        {
            {
                nameof(HttpClientErrorUris.InvalidSuccessContent),
                HttpClientErrorUris.InvalidSuccessContent,
                "tag:mapledev.engineer,2026-03-03:result.httpClient.invalidSuccessContent"
            },
            {
                nameof(HttpClientErrorUris.UnreadableContent),
                HttpClientErrorUris.UnreadableContent,
                "tag:mapledev.engineer,2026-03-03:result.httpClient.unreadableContent"
            },
        };

    [Theory]
    [MemberData(nameof(TypeUris))]
    public void TypeUri_Member_HasExactText(string member, ErrorUri typeUri, string expected)
    {
        // Act & Assert
        typeUri.Value.ShouldBe(expected, member);
    }

    // A tag URI (RFC 4151): ErrorUri.Tag validates the syntax and throws for any other text.
    [Theory]
    [MemberData(nameof(TypeUris))]
    public void TypeUri_Member_IsTagUri(string member, ErrorUri typeUri, string expected)
    {
        // Act
        var exception = Record.Exception(() => ErrorUri.Tag(expected));

        // Assert
        exception.ShouldBeNull(member);
        ErrorUri.Tag(expected).ShouldBe(typeUri, member);
    }

    // The value parses as the same tag URI, so a type that is read back from problem details equals the property.
    [Theory]
    [MemberData(nameof(TypeUris))]
    public void TypeUri_Member_EqualsTheParsedValue(string member, ErrorUri typeUri, string expected)
    {
        // Act
        var result = ErrorUri.Parse(expected);

        // Assert
        result.ShouldBe(typeUri, member);
    }

    [Fact]
    public void TypeUri_ReadTwice_IsTheSameValue()
    {
        // Act
        var first = HttpClientErrorUris.UnreadableContent;
        var second = HttpClientErrorUris.UnreadableContent;

        // Assert
        second.ShouldBe(first);
    }
}
