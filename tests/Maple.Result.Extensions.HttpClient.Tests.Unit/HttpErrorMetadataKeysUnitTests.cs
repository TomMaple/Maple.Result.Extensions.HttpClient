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
/// The keys are part of the wire format: an error written as problem details has them as member names, so a change is a breaking change.
/// </summary>
public class HttpErrorMetadataKeysUnitTests
{
    public static TheoryData<string, string, string> Keys
        => new()
        {
            { nameof(HttpErrorMetadataKeys.ExceptionType), HttpErrorMetadataKeys.ExceptionType, "exceptionType" },
            { nameof(HttpErrorMetadataKeys.HttpRequestError), HttpErrorMetadataKeys.HttpRequestError, "httpRequestError" },
            { nameof(HttpErrorMetadataKeys.HttpStatus), HttpErrorMetadataKeys.HttpStatus, "httpStatus" },
        };

    [Theory]
    [MemberData(nameof(Keys))]
    public void Key_Member_HasExactText(string member, string key, string expected)
    {
        // Act & Assert
        key.ShouldBe(expected, member);
    }

    // The core rejects a key that is blank, has white space at its ends or is a member of problem details.
    [Theory]
    [MemberData(nameof(Keys))]
    public void Key_Member_IsAcceptedByTheCore(string member, string key, string expected)
    {
        // Arrange
        var builder = ErrorBuilder.Failure(ErrorUri.None, "Title");

        // Act
        var exception = Record.Exception(() => builder.WithMetadata(key, 1));

        // Assert
        exception.ShouldBeNull(member);
        builder.Build().Metadata.Keys.ShouldContain(expected);
    }
}
