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

public class HttpClientErrorsUnitTests
{
    #region Uri

    // The types are part of the wire format: callers and other services recognise an error by them, so a change is a breaking change.
    public static TheoryData<string, ErrorUri, string> TypeUris
        => new()
        {
            {
                nameof(HttpClientErrors.InvalidSuccessContent),
                HttpClientErrors.InvalidSuccessContent.Uri,
                "tag:mapledev.engineer,2026-03-03:result.httpClient.invalidSuccessContent"
            },
            {
                nameof(HttpClientErrors.UnreadableContent),
                HttpClientErrors.UnreadableContent.Uri,
                "tag:mapledev.engineer,2026-03-03:result.httpClient.unreadableContent"
            },
        };

    [Theory]
    [MemberData(nameof(TypeUris))]
    public void Uri_Error_HasExactText(string error, ErrorUri typeUri, string expected)
    {
        // Act & Assert
        typeUri.Value.ShouldBe(expected, error);
    }

    // A tag URI (RFC 4151): ErrorUri.Tag validates the syntax and throws for any other text.
    [Theory]
    [MemberData(nameof(TypeUris))]
    public void Uri_Error_IsTagUri(string error, ErrorUri typeUri, string expected)
    {
        // Act
        var exception = Record.Exception(() => ErrorUri.Tag(expected));

        // Assert
        exception.ShouldBeNull(error);
        ErrorUri.Tag(expected).ShouldBe(typeUri, error);
    }

    // The value parses as the same tag URI, so a type that is read back from problem details equals the property.
    [Theory]
    [MemberData(nameof(TypeUris))]
    public void Uri_Error_EqualsTheParsedValue(string error, ErrorUri typeUri, string expected)
    {
        // Act
        var result = ErrorUri.Parse(expected);

        // Assert
        result.ShouldBe(typeUri, error);
    }

    [Fact]
    public void Uri_ReadTwice_IsTheSameValue()
    {
        // Act
        var first = HttpClientErrors.UnreadableContent.Uri;
        var second = HttpClientErrors.UnreadableContent.Uri;

        // Assert
        second.ShouldBe(first);
    }

    #endregion

    #region Title

    // The title of a type does not change from one occurrence to the next (RFC 9457).
    public static TheoryData<string, string, string> Titles
        => new()
        {
            {
                nameof(HttpClientErrors.InvalidSuccessContent),
                HttpClientErrors.InvalidSuccessContent.Title,
                "The HTTP response content could not be deserialized."
            },
            {
                nameof(HttpClientErrors.UnreadableContent),
                HttpClientErrors.UnreadableContent.Title,
                "The HTTP response content could not be read."
            },
        };

    [Theory]
    [MemberData(nameof(Titles))]
    public void Title_Error_HasExactText(string error, string title, string expected)
    {
        // Act & Assert
        title.ShouldBe(expected, error);
    }

    // The core rejects a blank title.
    [Theory]
    [MemberData(nameof(Titles))]
    public void Title_Error_IsAcceptedByTheCore(string error, string title, string expected)
    {
        // Act
        var exception = Record.Exception(() => ErrorBuilder.Failure(ErrorUri.None, title));

        // Assert
        exception.ShouldBeNull(error);
        title.ShouldBe(expected);
    }

    #endregion
}
