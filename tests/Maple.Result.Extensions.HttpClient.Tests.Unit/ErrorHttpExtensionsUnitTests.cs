// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Net;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit;

public class ErrorHttpExtensionsUnitTests
{
    #region TryGetHttpStatusCode

    [Fact]
    public void TryGetHttpStatusCode_NullError_ThrowsArgumentNullException()
    {
        // Arrange
        Error error = null!;

        // Act
        var exception = Record.Exception(() => error.TryGetHttpStatusCode(out _));

        // Assert
        var argumentException = exception.ShouldBeOfType<ArgumentNullException>();
        argumentException.ParamName.ShouldBe("error");
    }

    [Fact]
    public void TryGetHttpStatusCode_NoEntry_ReturnsFalse()
    {
        // Arrange
        var error = CreateError();

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeFalse();
        statusCode.ShouldBe(default(HttpStatusCode));
    }

    [Fact]
    public void TryGetHttpStatusCode_IntEntry_ReturnsTheStatusCode()
    {
        // Arrange
        var error = CreateError(HttpErrorMetadataKeys.HttpStatusCode, 404);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // A key taken from a response body may have another case; the lookups of the core ignore it.
    [Theory]
    [InlineData("HttpStatusCode")]
    [InlineData("HTTPSTATUSCODE")]
    [InlineData("httpstatuscode")]
    public void TryGetHttpStatusCode_KeyInAnotherCase_ReturnsTheStatusCode(string key)
    {
        // Arrange
        var error = CreateError(key, 503);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    // A number comes back from JSON in whatever type the deserializer chose.
    public static TheoryData<object> WholeNumbers
        => new()
        {
            429L,
            (short)429,
            (ushort)429,
            429U,
            429UL,
            429m,
            429d,
            429f,
        };

    [Theory]
    [MemberData(nameof(WholeNumbers))]
    public void TryGetHttpStatusCode_WholeNumberOfAnotherType_ReturnsTheStatusCode(object value)
    {
        // Arrange
        var error = CreateError(HttpErrorMetadataKeys.HttpStatusCode, value);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // A whole decimal with trailing zeros, which a conversion to a double turns into a fraction (403.99999999999994 and
    // 404.00000000000006): the decimal is checked in its own type.
    public static TheoryData<decimal> WholeDecimalsWithTrailingZeros
        => new()
        {
            404.00000000000000000000m,
            404.00000000000000000000000m,
            404.0000000000000000000000000m,
        };

    [Theory]
    [MemberData(nameof(WholeDecimalsWithTrailingZeros))]
    public void TryGetHttpStatusCode_WholeDecimalWithTrailingZeros_ReturnsTheStatusCode(decimal value)
    {
        // Arrange
        var error = CreateError(HttpErrorMetadataKeys.HttpStatusCode, value);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData((byte)200, HttpStatusCode.OK)]
    [InlineData((sbyte)100, HttpStatusCode.Continue)]
    public void TryGetHttpStatusCode_WholeNumberOfAByteType_ReturnsTheStatusCode(object value, HttpStatusCode expected)
    {
        // Arrange
        var error = CreateError(HttpErrorMetadataKeys.HttpStatusCode, value);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeTrue();
        statusCode.ShouldBe(expected);
    }

    // The range of the status codes that an HttpResponseMessage can have.
    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void TryGetHttpStatusCode_BoundaryOfTheRange_ReturnsTheStatusCode(int value)
    {
        // Arrange
        var error = CreateError(HttpErrorMetadataKeys.HttpStatusCode, value);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeTrue();
        statusCode.ShouldBe((HttpStatusCode)value);
    }

    public static TheoryData<object?> UnreadableValues
        => new()
        {
            (object?)null,
            "404",
            true,
            -1,
            1000,
            (long)int.MaxValue + 404,
            long.MaxValue,
            ulong.MaxValue,
            404.5d,
            404.5m,
            404.5f,
            // A fraction that a conversion to a double loses (it gives 404): the decimal is checked in its own type.
            404.0000000000000001m,
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity,
            float.NaN,
            float.PositiveInfinity,
            decimal.MaxValue,
            new[] { 404 },
        };

    [Theory]
    [MemberData(nameof(UnreadableValues))]
    public void TryGetHttpStatusCode_ValueThatIsNotAStatusCode_ReturnsFalse(object? value)
    {
        // Arrange
        var error = CreateError(HttpErrorMetadataKeys.HttpStatusCode, value);

        // Act
        var result = error.TryGetHttpStatusCode(out var statusCode);

        // Assert
        result.ShouldBeFalse();
        statusCode.ShouldBe(default(HttpStatusCode));
    }

    #endregion

    #region helper methods

    private static Error CreateError()
    {
        return Error.Failure(ErrorUri.None, "Title");
    }

    private static Error CreateError(string key, object? value)
    {
        return ErrorBuilder
            .Failure(ErrorUri.None, "Title")
            .WithMetadata(key, value)
            .Build();
    }

    #endregion
}
