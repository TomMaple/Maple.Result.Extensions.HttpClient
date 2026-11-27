// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Helpers;
using Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.TestData;
using System.Net;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.HttpResponseMessageExtensionsTests;

/// <summary>
/// A response with an error status code, mapped without a function of the caller: the status code decides the category and the title.
/// </summary>
public class HttpResponseMessageExtensionsErrorUnitTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region the error of the status code

    [Theory]
    [InlineData(400, ErrorCategory.Validation, "Bad Request")]
    [InlineData(401, ErrorCategory.Unauthenticated, "Unauthorized")]
    [InlineData(403, ErrorCategory.PermissionDenied, "Forbidden")]
    [InlineData(404, ErrorCategory.NotFound, "Not Found")]
    [InlineData(408, ErrorCategory.Timeout, "Request Timeout")]
    [InlineData(409, ErrorCategory.Conflict, "Conflict")]
    [InlineData(410, ErrorCategory.NotFound, "Gone")]
    [InlineData(413, ErrorCategory.Validation, "Content Too Large")]
    [InlineData(422, ErrorCategory.Failure, "Unprocessable Content")]
    [InlineData(429, ErrorCategory.LimitExceeded, "Too Many Requests")]
    [InlineData(500, ErrorCategory.Critical, "Internal Server Error")]
    [InlineData(501, ErrorCategory.NotImplemented, "Not Implemented")]
    [InlineData(502, ErrorCategory.Unavailable, "Bad Gateway")]
    [InlineData(503, ErrorCategory.Unavailable, "Service Unavailable")]
    [InlineData(504, ErrorCategory.Timeout, "Gateway Timeout")]
    [InlineData(507, ErrorCategory.Critical, "Insufficient Storage")]
    public async Task ToResultAsync_ErrorStatusCode_ReturnsTheErrorOfTheStatusCode(int statusCode, ErrorCategory category, string title)
    {
        // Arrange
        using var response = ResponseFactory.Create((HttpStatusCode)statusCode);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(category);
        result.Error.Title.ShouldBe(title);
        result.Error.TypeUri.ShouldBe(ErrorUri.None);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    // A redirect that was not followed and a code that is not registered are errors too: every status code that is not a success one is.
    [Theory]
    [InlineData(304, ErrorCategory.Failure, "Not Modified")]
    [InlineData(418, ErrorCategory.Failure, "Unknown Error")]
    [InlineData(599, ErrorCategory.Critical, "Unknown Error")]
    [InlineData(999, ErrorCategory.Failure, "Unknown Error")]
    public async Task ToResultAsync_OtherStatusCodeThatIsNotASuccess_ReturnsAnError(int statusCode, ErrorCategory category, string title)
    {
        // Arrange
        using var response = ResponseFactory.Create((HttpStatusCode)statusCode);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(category);
        result.Error.Title.ShouldBe(title);
    }

    [Fact]
    public async Task ToResultAsync_ErrorStatusCode_StoresTheStatusCodeAsTheOnlyMetadata()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(1);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
        result.Error.TryGetHttpStatusCode(out var statusCode).ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToResultAsyncOfT_ErrorStatusCode_ReturnsTheErrorOfTheStatusCode()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.Title.ShouldBe("Service Unavailable");
        result.Error.TypeUri.ShouldBe(ErrorUri.None);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(503);
    }

    // The metadata of T is needed only for a value: an error response is mapped also when the options do not have it.
    [Fact]
    public async Task ToResultAsyncOfT_ErrorStatusCodeAndNoMetadataOfT_ReturnsTheError()
    {
        // Arrange
        var jsonSerializerOptions = new JsonSerializerOptions { TypeInfoResolver = TestJsonContext.Default };
        var options = new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync<Uri>(options, CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
    }

    #endregion

    #region title

    // Regression (2.x used a body that was not an error object as the title, e.g., "null").
    [Theory]
    [InlineData("null", "application/json")]
    [InlineData("\"Something broke.\"", "application/json")]
    [InlineData("Something broke.", "text/plain")]
    [InlineData("<html><body><h1>502 Bad Gateway</h1></body></html>", "text/html")]
    [InlineData("Something broke.", null)]
    public async Task ToResultAsync_BodyThatIsNotAnErrorObject_IsNotUsedAsTheTitle(string body, string? contentType)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.InternalServerError, body, contentType);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe("Internal Server Error");
        result.Error.Detail.ShouldBeNull();
        result.Error.TypeUri.ShouldBe(ErrorUri.None);
    }

    // HTTP/2 and HTTP/3 have no reason phrase, and a server can send any text.
    [Fact]
    public async Task ToResultAsync_ReasonPhraseOfTheResponse_IsNotUsedAsTheTitle()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);
        response.ReasonPhrase = "Nothing To See Here";

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe("Not Found");
    }

    #endregion

    #region content

    // An error page of a proxy in a character set that the runtime does not know must not make the call throw.
    [Theory]
    [InlineData("text/html; charset=windows-1252")]
    [InlineData("text/html; charset=utf8")]
    [InlineData("application/json; charset=bogus")]
    public async Task ToResultAsync_ErrorBodyWithAnUnknownCharacterSet_ReturnsTheErrorOfTheStatusCode(string contentType)
    {
        // Arrange
        string? content = null;
        var options = new HttpResultOptions { ConfigureError = (context, _) => content = context.Content };
        using var response = ResponseFactory.Create(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>", contentType);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.Title.ShouldBe("Bad Gateway");
        content.ShouldBe("<html>Bad Gateway</html>");
    }

    #endregion

    #region MapStatus

    [Fact]
    public async Task ToResultAsync_MapStatusReturnsACategory_UsesThatCategory()
    {
        // Arrange
        HttpStatusCode? received = null;
        var options = new HttpResultOptions
        {
            MapStatus = statusCode =>
            {
                received = statusCode;
                return ErrorCategory.Conflict;
            }
        };
        using var response = ResponseFactory.Create(HttpStatusCode.Gone);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        received.ShouldBe(HttpStatusCode.Gone);
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Conflict);
        result.Error.Title.ShouldBe("Gone");
    }

    [Fact]
    public async Task ToResultAsync_MapStatusReturnsNull_UsesTheCategoryOfTheDefaultMap()
    {
        // Arrange
        var options = new HttpResultOptions { MapStatus = _ => null };
        using var response = ResponseFactory.Create(HttpStatusCode.Gone);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
    }

    #endregion

    #region ConfigureError

    [Fact]
    public async Task ToResultAsync_ConfigureError_ReceivesTheContextOfTheResponse()
    {
        // Arrange
        var body = """{"message":"Not here."}""";
        HttpErrorContext? received = null;
        var options = new HttpResultOptions { ConfigureError = (context, _) => received = context };
        var requestUri = new Uri("https://orders.example.com/orders/42");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, body);
        response.RequestMessage = request;

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        received.ShouldNotBeNull();
        received.Response.ShouldBeSameAs(response);
        received.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        received.RequestUri.ShouldBe(requestUri);
        received.Content.ShouldBe(body);
        received.DefaultCategory.ShouldBe(ErrorCategory.NotFound);
        received.DefaultTitle.ShouldBe("Not Found");
    }

    [Fact]
    public async Task ToResultAsync_ConfigureErrorAndAResponseWithoutABody_ReceivesEmptyContent()
    {
        // Arrange
        string? content = null;
        var options = new HttpResultOptions { ConfigureError = (context, _) => content = context.Content };
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        content.ShouldBe(string.Empty);
    }

    // The category of the context is the one that MapStatus chose.
    [Fact]
    public async Task ToResultAsync_ConfigureErrorAndMapStatus_ReceivesTheMappedCategory()
    {
        // Arrange
        ErrorCategory? category = null;
        var options = new HttpResultOptions
        {
            MapStatus = _ => ErrorCategory.Timeout,
            ConfigureError = (context, _) => category = context.DefaultCategory
        };
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        category.ShouldBe(ErrorCategory.Timeout);
    }

    [Fact]
    public async Task ToResultAsync_ConfigureError_ChangesTheError()
    {
        // Arrange
        var options = new HttpResultOptions
        {
            ConfigureError = (context, builder) =>
            {
                var correlationId = context.Response.Headers.GetValues("X-Correlation-Id").First();
                builder
                    .WithMetadata("correlationId", correlationId)
                    .WithDetail("See the correlation identifier.");
            }
        };
        using var response = ResponseFactory.Create(HttpStatusCode.InternalServerError);
        response.Headers.Add("X-Correlation-Id", "c-123");

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Detail.ShouldBe("See the correlation identifier.");
        result.Error.Metadata.Keys.ShouldContain("correlationId");
        result.Error.Metadata["correlationId"].ShouldBe("c-123");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
    }

    // It runs last: the status code is there already, and what the function sets is kept.
    [Fact]
    public async Task ToResultAsync_ConfigureError_RunsAfterTheStatusCodeIsStored()
    {
        // Arrange
        object? statusWhenConfigured = null;
        var options = new HttpResultOptions
        {
            ConfigureError = (_, builder) =>
            {
                var error = builder.Build();
                error.Metadata.TryGetValue(HttpErrorMetadataKeys.HttpStatusCode, out statusWhenConfigured);
                builder.WithMetadata(HttpErrorMetadataKeys.HttpStatusCode, 418);
            }
        };
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        statusWhenConfigured.ShouldBe(404);
        result.Error.ShouldNotBeNull();
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(418);
    }

    [Fact]
    public async Task ToResultAsyncOfT_ConfigureError_ChangesTheError()
    {
        // Arrange
        var options = new HttpResultOptions { ConfigureError = (_, builder) => builder.WithMetadata("traceId", "abc") };
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Keys.ShouldContain("traceId");
        result.Error.Metadata["traceId"].ShouldBe("abc");
    }

    // A function of the caller that throws has a bug: the exception is not turned into an error.
    [Fact]
    public async Task ToResultAsync_ConfigureErrorThrows_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new InvalidOperationException("A bug in the function.");
        var options = new HttpResultOptions { ConfigureError = (_, _) => throw thrown };
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync(options, CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    #endregion
}
