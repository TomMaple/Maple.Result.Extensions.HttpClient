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
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit;

public class HttpResultOptionsUnitTests
{
    #region constructor

    [Fact]
    public void Ctor_NoArguments_HasTheDefaultSettings()
    {
        // Act
        var sut = new HttpResultOptions();

        // Assert
        sut.JsonSerializerOptions.ShouldBeSameAs(JsonSerializerOptions.Web);
        sut.MapStatus.ShouldBeNull();
        sut.ConfigureError.ShouldBeNull();
    }

    #endregion

    #region ConfigureError

    [Fact]
    public void ConfigureError_Set_IsReturned()
    {
        // Arrange
        var sut = new HttpResultOptions();
        Action<HttpErrorContext, ErrorBuilder> configureError = (_, builder) => builder.WithMetadata("traceId", "abc");

        // Act
        sut.ConfigureError = configureError;

        // Assert
        sut.ConfigureError.ShouldBeSameAs(configureError);
    }

    [Fact]
    public void ConfigureError_SetOnReadOnlyOptions_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new HttpResultOptions();
        sut.MakeReadOnly();

        // Act
        var exception = Record.Exception(() => sut.ConfigureError = (_, _) => { });

        // Assert
        exception.ShouldBeOfType<InvalidOperationException>();
        sut.ConfigureError.ShouldBeNull();
    }

    #endregion

    #region Default

    [Fact]
    public void Default_ReadTwice_IsTheSameInstance()
    {
        // Act
        var first = HttpResultOptions.Default;
        var second = HttpResultOptions.Default;

        // Assert
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void Default_Instance_HasTheDefaultSettings()
    {
        // Act
        var sut = HttpResultOptions.Default;

        // Assert
        sut.JsonSerializerOptions.ShouldBeSameAs(JsonSerializerOptions.Web);
        sut.MapStatus.ShouldBeNull();
        sut.ConfigureError.ShouldBeNull();
    }

    // The default instance is shared by every call without options, so it cannot be changed.
    [Fact]
    public void Default_Set_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = HttpResultOptions.Default;

        // Act
        var exception = Record.Exception(() => sut.MapStatus = _ => ErrorCategory.Critical);

        // Assert
        exception.ShouldBeOfType<InvalidOperationException>();
        sut.MapStatus.ShouldBeNull();
    }

    #endregion

    #region DefaultStatusMap

    // The column "status read (default)" of the problem details format of the core.
    [Theory]
    [InlineData(400, ErrorCategory.Validation)]
    [InlineData(413, ErrorCategory.Validation)]
    [InlineData(414, ErrorCategory.Validation)]
    [InlineData(431, ErrorCategory.Validation)]
    [InlineData(401, ErrorCategory.Unauthenticated)]
    [InlineData(403, ErrorCategory.PermissionDenied)]
    [InlineData(404, ErrorCategory.NotFound)]
    [InlineData(410, ErrorCategory.NotFound)]
    [InlineData(408, ErrorCategory.Timeout)]
    [InlineData(504, ErrorCategory.Timeout)]
    [InlineData(409, ErrorCategory.Conflict)]
    [InlineData(412, ErrorCategory.Conflict)]
    [InlineData(423, ErrorCategory.Conflict)]
    [InlineData(422, ErrorCategory.Failure)]
    [InlineData(429, ErrorCategory.LimitExceeded)]
    [InlineData(500, ErrorCategory.Critical)]
    [InlineData(501, ErrorCategory.NotImplemented)]
    [InlineData(502, ErrorCategory.Unavailable)]
    [InlineData(503, ErrorCategory.Unavailable)]
    public void DefaultStatusMap_ListedStatusCode_ReturnsItsCategory(int statusCode, ErrorCategory expected)
    {
        // Act
        var result = HttpResultOptions.DefaultStatusMap((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(402)]
    [InlineData(405)]
    [InlineData(406)]
    [InlineData(415)]
    [InlineData(418)]
    [InlineData(451)]
    [InlineData(499)]
    public void DefaultStatusMap_AnyOtherClientError_ReturnsFailure(int statusCode)
    {
        // Act
        var result = HttpResultOptions.DefaultStatusMap((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe(ErrorCategory.Failure);
    }

    // A server error is never an expected error (Failure).
    [Theory]
    [InlineData(505)]
    [InlineData(507)]
    [InlineData(511)]
    [InlineData(599)]
    public void DefaultStatusMap_AnyOtherServerError_ReturnsCritical(int statusCode)
    {
        // Act
        var result = HttpResultOptions.DefaultStatusMap((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe(ErrorCategory.Critical);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(304)]
    [InlineData(399)]
    [InlineData(600)]
    [InlineData(999)]
    public void DefaultStatusMap_CodeOutsideTheErrorClasses_ReturnsFailure(int statusCode)
    {
        // Act
        var result = HttpResultOptions.DefaultStatusMap((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe(ErrorCategory.Failure);
    }

    #endregion

    #region GetCategory

    [Fact]
    public void GetCategory_NoMapStatus_ReturnsTheCategoryOfTheDefaultMap()
    {
        // Arrange
        var sut = new HttpResultOptions();

        // Act
        var result = sut.GetCategory(HttpStatusCode.TooManyRequests);

        // Assert
        result.ShouldBe(ErrorCategory.LimitExceeded);
    }

    [Fact]
    public void GetCategory_MapStatusReturnsACategory_ReturnsThatCategory()
    {
        // Arrange
        var sut = new HttpResultOptions { MapStatus = _ => ErrorCategory.Conflict };

        // Act
        var result = sut.GetCategory(HttpStatusCode.TooManyRequests);

        // Assert
        result.ShouldBe(ErrorCategory.Conflict);
    }

    [Fact]
    public void GetCategory_MapStatus_ReceivesTheStatusCode()
    {
        // Arrange
        HttpStatusCode? received = null;
        var sut = new HttpResultOptions
        {
            MapStatus = statusCode =>
            {
                received = statusCode;
                return null;
            }
        };

        // Act
        sut.GetCategory(HttpStatusCode.Gone);

        // Assert
        received.ShouldBe(HttpStatusCode.Gone);
    }

    [Fact]
    public void GetCategory_MapStatusReturnsNull_ReturnsTheCategoryOfTheDefaultMap()
    {
        // Arrange
        var sut = new HttpResultOptions { MapStatus = _ => null };

        // Act
        var result = sut.GetCategory(HttpStatusCode.BadGateway);

        // Assert
        result.ShouldBe(ErrorCategory.Unavailable);
    }

    [Fact]
    public void GetCategory_MapStatusReturnsAnUndefinedCategory_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new HttpResultOptions { MapStatus = _ => (ErrorCategory)123 };

        // Act
        var exception = Record.Exception(() => sut.GetCategory(HttpStatusCode.BadGateway));

        // Assert
        var invalidOperationException = exception.ShouldBeOfType<InvalidOperationException>();
        invalidOperationException.Message.ShouldContain(nameof(HttpResultOptions.MapStatus));
        invalidOperationException.Message.ShouldContain("123");
        invalidOperationException.Message.ShouldContain("502");
    }

    #endregion

    #region JsonSerializerOptions

    [Fact]
    public void JsonSerializerOptions_Set_IsReturned()
    {
        // Arrange
        var sut = new HttpResultOptions();
        var jsonSerializerOptions = new JsonSerializerOptions();

        // Act
        sut.JsonSerializerOptions = jsonSerializerOptions;

        // Assert
        sut.JsonSerializerOptions.ShouldBeSameAs(jsonSerializerOptions);
    }

    [Fact]
    public void JsonSerializerOptions_SetNull_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new HttpResultOptions();

        // Act
        var exception = Record.Exception(() => sut.JsonSerializerOptions = null!);

        // Assert
        exception.ShouldBeOfType<ArgumentNullException>();
        sut.JsonSerializerOptions.ShouldBeSameAs(JsonSerializerOptions.Web);
    }

    [Fact]
    public void JsonSerializerOptions_SetOnReadOnlyOptions_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new HttpResultOptions();
        sut.MakeReadOnly();

        // Act
        var exception = Record.Exception(() => sut.JsonSerializerOptions = new JsonSerializerOptions());

        // Assert
        exception.ShouldBeOfType<InvalidOperationException>();
        sut.JsonSerializerOptions.ShouldBeSameAs(JsonSerializerOptions.Web);
    }

    #endregion

    #region MakeReadOnly

    [Fact]
    public void MakeReadOnly_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var sut = new HttpResultOptions();
        sut.MakeReadOnly();

        // Act
        var exception = Record.Exception(sut.MakeReadOnly);

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void MakeReadOnly_ReadOnlyOptions_KeepTheirSettings()
    {
        // Arrange
        Func<HttpStatusCode, ErrorCategory?> mapStatus = _ => ErrorCategory.Timeout;
        Action<HttpErrorContext, ErrorBuilder> configureError = (_, _) => { };
        var jsonSerializerOptions = new JsonSerializerOptions();
        var sut = new HttpResultOptions
        {
            JsonSerializerOptions = jsonSerializerOptions,
            MapStatus = mapStatus,
            ConfigureError = configureError
        };

        // Act
        sut.MakeReadOnly();

        // Assert
        sut.JsonSerializerOptions.ShouldBeSameAs(jsonSerializerOptions);
        sut.MapStatus.ShouldBeSameAs(mapStatus);
        sut.ConfigureError.ShouldBeSameAs(configureError);
    }

    // As the JsonSerializer does on first use: options without a resolver get the reflection-based one where reflection is enabled,
    // so that new JsonSerializerOptions { … } works as it does with JsonSerializer.Deserialize.
    [Fact]
    public void MakeReadOnly_JsonSerializerOptionsWithoutAResolver_MakesThemReadOnlyWithTheDefaultResolver()
    {
        // Arrange
        var jsonSerializerOptions = new JsonSerializerOptions();
        var sut = new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };

        // Act
        sut.MakeReadOnly();

        // Assert
        jsonSerializerOptions.IsReadOnly.ShouldBeTrue();
        jsonSerializerOptions.TypeInfoResolver.ShouldBeOfType<DefaultJsonTypeInfoResolver>();
    }

    [Fact]
    public void MakeReadOnly_JsonSerializerOptionsWithAResolver_KeepsTheResolver()
    {
        // Arrange
        var resolver = JsonTypeInfoResolver.Combine();
        var jsonSerializerOptions = new JsonSerializerOptions { TypeInfoResolver = resolver };
        var sut = new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };

        // Act
        sut.MakeReadOnly();

        // Assert
        jsonSerializerOptions.IsReadOnly.ShouldBeTrue();
        jsonSerializerOptions.TypeInfoResolver.ShouldBeSameAs(resolver);
    }

    [Fact]
    public void MakeReadOnly_ReadOnlyJsonSerializerOptions_LeavesThemUnchanged()
    {
        // Arrange
        var resolver = JsonSerializerOptions.Web.TypeInfoResolver;
        var sut = new HttpResultOptions { JsonSerializerOptions = JsonSerializerOptions.Web };

        // Act
        sut.MakeReadOnly();

        // Assert
        JsonSerializerOptions.Web.IsReadOnly.ShouldBeTrue();
        JsonSerializerOptions.Web.TypeInfoResolver.ShouldBeSameAs(resolver);
    }

    #endregion

    #region MapStatus

    [Fact]
    public void MapStatus_Set_IsReturned()
    {
        // Arrange
        var sut = new HttpResultOptions();
        Func<HttpStatusCode, ErrorCategory?> mapStatus = _ => ErrorCategory.Timeout;

        // Act
        sut.MapStatus = mapStatus;

        // Assert
        sut.MapStatus.ShouldBeSameAs(mapStatus);
    }

    [Fact]
    public void MapStatus_SetOnReadOnlyOptions_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new HttpResultOptions();
        sut.MakeReadOnly();

        // Act
        var exception = Record.Exception(() => sut.MapStatus = _ => ErrorCategory.Timeout);

        // Assert
        var invalidOperationException = exception.ShouldBeOfType<InvalidOperationException>();
        invalidOperationException.Message.ShouldContain("read-only");
        sut.MapStatus.ShouldBeNull();
    }

    #endregion

    #region RequestOptionsKey

    // Other code that shares the options of a request must not use the same key by accident.
    [Fact]
    public void RequestOptionsKey_Key_IsTheFullNameOfTheOptionsType()
    {
        // Act
        var result = HttpResultOptions.RequestOptionsKey.Key;

        // Assert
        result.ShouldBe("Maple.Results.Extensions.HttpClient.HttpResultOptions");
    }

    #endregion

    #region Resolve

    [Fact]
    public void Resolve_OptionsArgument_ReturnsTheArgument()
    {
        // Arrange
        var argument = new HttpResultOptions();
        var ofTheRequest = new HttpResultOptions();
        using var request = new HttpRequestMessage();
        request.Options.Set(HttpResultOptions.RequestOptionsKey, ofTheRequest);
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.RequestMessage = request;

        // Act
        var result = HttpResultOptions.Resolve(response, argument);

        // Assert
        result.ShouldBeSameAs(argument);
    }

    [Fact]
    public void Resolve_NoArgumentAndOptionsOfTheRequest_ReturnsTheOptionsOfTheRequest()
    {
        // Arrange
        var ofTheRequest = new HttpResultOptions();
        using var request = new HttpRequestMessage();
        request.Options.Set(HttpResultOptions.RequestOptionsKey, ofTheRequest);
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.RequestMessage = request;

        // Act
        var result = HttpResultOptions.Resolve(response, null);

        // Assert
        result.ShouldBeSameAs(ofTheRequest);
    }

    [Fact]
    public void Resolve_NoArgumentAndARequestWithoutOptions_ReturnsTheDefault()
    {
        // Arrange
        using var request = new HttpRequestMessage();
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.RequestMessage = request;

        // Act
        var result = HttpResultOptions.Resolve(response, null);

        // Assert
        result.ShouldBeSameAs(HttpResultOptions.Default);
    }

    [Fact]
    public void Resolve_NoArgumentAndNoRequest_ReturnsTheDefault()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var result = HttpResultOptions.Resolve(response, null);

        // Assert
        result.ShouldBeSameAs(HttpResultOptions.Default);
    }

    // The options are frozen when they are first used, as JsonSerializerOptions are.
    [Fact]
    public void Resolve_MutableOptions_MakesThemReadOnly()
    {
        // Arrange
        var options = new HttpResultOptions();
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        HttpResultOptions.Resolve(response, options);

        // Assert
        var exception = Record.Exception(() => options.MapStatus = _ => null);
        exception.ShouldBeOfType<InvalidOperationException>();
    }

    #endregion
}
