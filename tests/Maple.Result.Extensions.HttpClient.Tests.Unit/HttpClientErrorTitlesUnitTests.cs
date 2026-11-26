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
/// The titles belong to the types of <see cref="HttpClientErrorUris" />: a title of a type does not change from one occurrence to the next (RFC 9457).
/// </summary>
public class HttpClientErrorTitlesUnitTests
{
    public static TheoryData<string, string, string> Titles
        => new()
        {
            {
                nameof(HttpClientErrorTitles.InvalidSuccessContent),
                HttpClientErrorTitles.InvalidSuccessContent,
                "The HTTP response content could not be deserialized."
            },
            {
                nameof(HttpClientErrorTitles.UnreadableContent),
                HttpClientErrorTitles.UnreadableContent,
                "The HTTP response content could not be read."
            },
        };

    [Theory]
    [MemberData(nameof(Titles))]
    public void Title_Member_HasExactText(string member, string title, string expected)
    {
        // Act & Assert
        title.ShouldBe(expected, member);
    }

    // The core rejects a blank title.
    [Theory]
    [MemberData(nameof(Titles))]
    public void Title_Member_IsAcceptedByTheCore(string member, string title, string expected)
    {
        // Act
        var exception = Record.Exception(() => ErrorBuilder.Failure(ErrorUri.None, title));

        // Assert
        exception.ShouldBeNull(member);
        title.ShouldBe(expected);
    }
}
