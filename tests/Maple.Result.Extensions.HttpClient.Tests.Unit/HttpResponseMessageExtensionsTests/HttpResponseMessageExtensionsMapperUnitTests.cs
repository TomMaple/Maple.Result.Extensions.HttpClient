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
/// A response with an error status code whose body is read as a type of the caller and mapped by a function of the caller:
/// one that changes a builder, or one that returns the error.
/// </summary>
public class HttpResponseMessageExtensionsMapperUnitTests
{
    private const string ErrorJson = """{"code":"E42","message":"The order is not here."}""";
    private const string OrderJson = """{"id":42,"customer":"Ada"}""";

    private static readonly ErrorUri OrderNotFound = ErrorUri.Tag("tag:acme.com,2026:orders.notFound");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region a function that changes the builder

    [Fact]
    public async Task ToResultAsync_BuilderFunction_ReceivesTheBodyAsTError()
    {
        // Arrange
        ApiError? received = null;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((body, _, _) => received = body, cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        received.ShouldBe(new ApiError("E42", "The order is not here."));
    }

    // The builder has the error of the status code, so a function sets only what the body adds.
    [Fact]
    public async Task ToResultAsync_BuilderFunctionThatChangesNothing_ReturnsTheErrorOfTheStatusCode()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((_, _, _) => { }, cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
        result.Error.TypeUri.ShouldBe(ErrorUri.None);
        result.Error.Title.ShouldBe("Not Found");
        result.Error.Detail.ShouldBeNull();
        result.Error.Metadata.Count.ShouldBe(1);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    [Fact]
    public async Task ToResultAsync_BuilderFunction_ChangesTheError()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context, builder) => builder
                .WithTypeUri(OrderNotFound)
                .WithTitle(body?.Message ?? context.DefaultTitle)
                .WithMetadata("code", body?.Code),
            cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
        result.Error.TypeUri.ShouldBe(OrderNotFound);
        result.Error.Title.ShouldBe("The order is not here.");
        result.Error.Metadata.Keys.ShouldContain("code");
        result.Error.Metadata["code"].ShouldBe("E42");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    [Fact]
    public async Task ToResultAsync_BuilderFunction_ReceivesTheContextOfTheResponse()
    {
        // Arrange
        HttpErrorContext? received = null;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.Conflict, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((_, context, _) => received = context, cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        received.ShouldNotBeNull();
        received.Response.ShouldBeSameAs(response);
        received.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        received.Content.ShouldBe(ErrorJson);
        received.DefaultCategory.ShouldBe(ErrorCategory.Conflict);
        received.DefaultTitle.ShouldBe("Conflict");
    }

    // A body that is not a TError is not an error by itself: the function gets null and the raw text.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("[1,2]")]
    [InlineData("""{"code":""")]
    [InlineData("<html><body>Bad Gateway</body></html>")]
    public async Task ToResultAsync_BodyThatIsNotATError_PassesNullAndTheRawText(string content)
    {
        // Arrange
        var called = false;
        ApiError? receivedBody = null;
        string? receivedContent = null;
        using var response = ResponseFactory.Create(HttpStatusCode.BadGateway, content, "application/json");

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context, _) =>
            {
                called = true;
                receivedBody = body;
                receivedContent = context.Content;
            },
            cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        called.ShouldBeTrue();
        receivedBody.ShouldBeNull();
        receivedContent.ShouldBe(content);
    }

    [Fact]
    public async Task ToResultAsync_ResponseWithoutABody_PassesNullAndEmptyText()
    {
        // Arrange
        var called = false;
        ApiError? receivedBody = null;
        string? receivedContent = null;
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context, _) =>
            {
                called = true;
                receivedBody = body;
                receivedContent = context.Content;
            },
            cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        called.ShouldBeTrue();
        receivedBody.ShouldBeNull();
        receivedContent.ShouldBe(string.Empty);
    }

    // For a value type, the default value stands for a body that could not be read.
    [Theory]
    [InlineData("7", 7)]
    [InlineData("not a number", 0)]
    public async Task ToResultAsync_ValueTypeAsTError_PassesTheValueOrTheDefault(string content, int expected)
    {
        // Arrange
        int? received = null;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.BadRequest, content);

        // Act
        var result = await response.ToResultAsync<int>((body, _, _) => received = body, cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        received.ShouldBe(expected);
    }

    [Fact]
    public async Task ToResultAsync_JsonSerializerOptionsOfTheOptions_ReadTheBodyOfTheError()
    {
        // Arrange
        ApiError? received = null;
        var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.KebabCaseUpper };
        var options = new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, """{"CODE":"E42","MESSAGE":"Not here."}""");

        // Act
        var result = await response.ToResultAsync<ApiError>((body, _, _) => received = body, options, CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        received.ShouldBe(new ApiError("E42", "Not here."));
    }

    [Fact]
    public async Task ToResultAsync_SourceGeneratedMetadataOfTError_ReadsTheBodyOfTheError()
    {
        // Arrange
        ApiError? received = null;
        var options = CreateSourceGeneratedOptions();
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((body, _, _) => received = body, options, CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        received.ShouldBe(new ApiError("E42", "The order is not here."));
    }

    // A type that the options cannot read is a mistake in the setup of the application: it is not hidden as a null body.
    [Fact]
    public async Task ToResultAsync_NoMetadataOfTError_ThrowsNotSupportedException()
    {
        // Arrange
        var options = CreateSourceGeneratedOptions();
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<OtherError>((_, _, _) => { }, options, CancellationToken));

        // Assert
        var notSupportedException = exception.ShouldBeOfType<NotSupportedException>();
        notSupportedException.Message.ShouldContain(nameof(OtherError));
    }

    // The entries about the response are stored after the function, so they replace what the function (or the body) stored under their keys.
    [Theory]
    [InlineData("httpStatusCode")]
    [InlineData("HttpStatusCode")]
    [InlineData("HTTPSTATUSCODE")]
    public async Task ToResultAsync_BuilderFunctionStoresTheKeyOfTheStatusCode_TheStatusCodeOfTheResponseReplacesIt(string key)
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((_, _, builder) => builder.WithMetadata(key, 200), cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(1);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    [Fact]
    public async Task ToResultAsync_BuilderFunctionAndConfigureError_ConfigureErrorRunsLast()
    {
        // Arrange
        var options = new HttpResultOptions { ConfigureError = (_, builder) => builder.WithTitle("Set by ConfigureError") };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((_, _, builder) => builder.WithTitle("Set by the function"), options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe("Set by ConfigureError");
    }

    // Regression (2.x reported an exception of the function of the caller as a deserialization error).
    [Fact]
    public async Task ToResultAsync_BuilderFunctionThrows_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new InvalidOperationException("A bug in the function.");
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var exception = await Record.ExceptionAsync(
            () => response.ToResultAsync<ApiError>((_, _, _) => throw thrown, cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    // An argument that the builder of the core rejects (here, a blank title) is a bug in the function as well.
    [Fact]
    public async Task ToResultAsync_BuilderFunctionPassesAnInvalidValue_ThrowsTheExceptionOfTheBuilder()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, """{"code":"E42","message":" "}""");

        // Act
        var exception = await Record.ExceptionAsync(
            () => response.ToResultAsync<ApiError>((body, _, builder) => builder.WithTitle(body?.Message ?? "?"), cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public async Task ToResultAsync_BuilderFunctionAndASuccessResponse_DoesNotCallTheFunction()
    {
        // Arrange
        var called = false;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>((_, _, _) => called = true, cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ToResultAsyncOfT_BuilderFunctionAndAnErrorResponse_ReturnsTheMappedError()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<Order, ApiError>(
            (body, context, builder) => builder.WithTitle(body?.Message ?? context.DefaultTitle),
            cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
        result.Error.Title.ShouldBe("The order is not here.");
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    [Fact]
    public async Task ToResultAsyncOfT_BuilderFunctionAndASuccessResponse_ReturnsTheValue()
    {
        // Arrange
        var called = false;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order, ApiError>((_, _, _) => called = true, cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
        called.ShouldBeFalse();
    }

    #endregion

    #region a function that returns the error

    [Fact]
    public async Task ToResultAsync_ErrorFunction_ReturnsItsErrorWithTheStatusCode()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context) => Error.NotFound(OrderNotFound, body?.Message ?? context.DefaultTitle, "Order 42."),
            cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
        result.Error.TypeUri.ShouldBe(OrderNotFound);
        result.Error.Title.ShouldBe("The order is not here.");
        result.Error.Detail.ShouldBe("Order 42.");
        result.Error.Metadata.Count.ShouldBe(1);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    // The category of the returned error is kept: the function decides everything but the entries about the response.
    [Fact]
    public async Task ToResultAsync_ErrorFunctionReturnsAnotherCategory_KeepsThatCategory()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (_, context) => Error.Conflict(OrderNotFound, context.DefaultTitle),
            cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Conflict);
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunction_ReceivesTheBodyAndTheContext()
    {
        // Arrange
        ApiError? receivedBody = null;
        HttpErrorContext? receivedContext = null;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context) =>
            {
                receivedBody = body;
                receivedContext = context;

                return Error.NotFound(OrderNotFound, context.DefaultTitle);
            },
            cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        receivedBody.ShouldBe(new ApiError("E42", "The order is not here."));
        receivedContext.ShouldNotBeNull();
        receivedContext.Response.ShouldBeSameAs(response);
        receivedContext.Content.ShouldBe(ErrorJson);
        receivedContext.DefaultCategory.ShouldBe(ErrorCategory.NotFound);
        receivedContext.DefaultTitle.ShouldBe("Not Found");
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunctionAndABodyThatIsNotATError_PassesNull()
    {
        // Arrange
        var called = false;
        ApiError? receivedBody = null;
        using var response = ResponseFactory.Create(HttpStatusCode.BadGateway, "<html>Bad Gateway</html>", "text/html");

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context) =>
            {
                called = true;
                receivedBody = body;

                return Error.Unavailable(ErrorUri.None, context.DefaultTitle);
            },
            cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        called.ShouldBeTrue();
        receivedBody.ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunctionReturnsAnErrorWithMetadata_KeepsItsMetadata()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (body, context) => Error
                .NotFound(OrderNotFound, context.DefaultTitle)
                .WithMetadata(("code", body?.Code), ("httpStatusCode", 200)),
            cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(2);
        result.Error.Metadata.Keys.ShouldContain("code");
        result.Error.Metadata["code"].ShouldBe("E42");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunctionAndConfigureError_ConfigureErrorChangesTheReturnedError()
    {
        // Arrange
        var options = new HttpResultOptions { ConfigureError = (_, builder) => builder.WithMetadata("traceId", "abc") };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (_, context) => Error.NotFound(OrderNotFound, context.DefaultTitle),
            options,
            CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(OrderNotFound);
        result.Error.Metadata.Keys.ShouldContain("traceId");
        result.Error.Metadata["traceId"].ShouldBe("abc");
    }

    // A function that returns no error has a bug; the message names the function, as the core does.
    [Fact]
    public async Task ToResultAsync_ErrorFunctionReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var exception = await Record.ExceptionAsync(
            () => response.ToResultAsync<ApiError>((_, _) => null!, cancellationToken: CancellationToken));

        // Assert
        var invalidOperationException = exception.ShouldBeOfType<InvalidOperationException>();
        invalidOperationException.Message.ShouldBe("The function ‘mapError’ returned null.");
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunctionThrows_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new InvalidOperationException("A bug in the function.");
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var exception = await Record.ExceptionAsync(
            () => response.ToResultAsync<ApiError>((_, _) => throw thrown, cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunctionAndASuccessResponse_DoesNotCallTheFunction()
    {
        // Arrange
        var called = false;
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, ErrorJson);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (_, context) =>
            {
                called = true;

                return Error.Failure(ErrorUri.None, context.DefaultTitle);
            },
            cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ToResultAsyncOfT_ErrorFunctionAndAnErrorResponse_ReturnsItsError()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync<Order, ApiError>(
            (body, context) => Error.NotFound(OrderNotFound, body?.Message ?? context.DefaultTitle),
            cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(OrderNotFound);
        result.Error.Title.ShouldBe("The order is not here.");
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    [Fact]
    public async Task ToResultAsyncOfT_ErrorFunctionAndASuccessResponse_ReturnsTheValue()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order, ApiError>(
            (_, context) => Error.Failure(ErrorUri.None, context.DefaultTitle),
            cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    #endregion

    #region helper methods

    private static HttpResultOptions CreateSourceGeneratedOptions()
    {
        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = TestJsonContext.Default };

        return new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };
    }

    #endregion

    private sealed record OtherError(string? Reason);
}
