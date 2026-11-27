// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Fakes;
using Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Helpers;
using Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.TestData;
using System.Net;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.HttpResponseMessageExtensionsTests;

/// <summary>
/// The status code and the headers have arrived, but reading the content fails (e.g., the connection drops): the result is an
/// Unavailable error, for a success and for an error status code alike.
/// </summary>
public class HttpResponseMessageExtensionsContentFailureUnitTests
{
    private const string ResponseEndedMessage = "The response ended prematurely.";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region the error

    [Fact]
    public async Task ToResultAsyncOfT_SuccessResponseWhoseContentCannotBeRead_ReturnsUnreadableContent()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new HttpIOException(HttpRequestError.ResponseEnded, ResponseEndedMessage));
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        result.Error.Title.ShouldBe(HttpClientErrors.UnreadableContent.Title);
        result.Error.Detail.ShouldNotBeNull();
        result.Error.Detail.ShouldContain(ResponseEndedMessage);
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    // Read as a stream, the content throws the exception of the connection as it is.
    [Fact]
    public async Task ToResultAsyncOfT_SuccessResponseWhoseContentCannotBeRead_StoresTheExceptionAndTheStatusCode()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new HttpIOException(HttpRequestError.ResponseEnded, ResponseEndedMessage));
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(3);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.ExceptionType);
        result.Error.Metadata[HttpErrorMetadataKeys.ExceptionType].ShouldBe("System.Net.Http.HttpIOException");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpRequestError);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpRequestError].ShouldBe("ResponseEnded");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatus);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatus].ShouldBe(200);
    }

    [Fact]
    public async Task ToResultAsync_ErrorResponseWhoseContentCannotBeRead_ReturnsUnreadableContent()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new HttpIOException(HttpRequestError.ResponseEnded, ResponseEndedMessage));
        using var response = ResponseFactory.Create(HttpStatusCode.BadGateway, content);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        result.Error.Title.ShouldBe(HttpClientErrors.UnreadableContent.Title);
        result.Error.Detail.ShouldNotBeNull();
    }

    // Read as text, HttpContent copies the body into its buffer first and wraps the exception of the connection in an
    // HttpRequestException, which keeps its HttpRequestError.
    [Fact]
    public async Task ToResultAsync_ErrorResponseWhoseContentCannotBeRead_StoresTheExceptionAndTheStatusCode()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new HttpIOException(HttpRequestError.ResponseEnded, ResponseEndedMessage));
        using var response = ResponseFactory.Create(HttpStatusCode.BadGateway, content);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(3);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.ExceptionType);
        result.Error.Metadata[HttpErrorMetadataKeys.ExceptionType].ShouldBe("System.Net.Http.HttpRequestException");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpRequestError);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpRequestError].ShouldBe("ResponseEnded");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatus);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatus].ShouldBe(502);
    }

    [Fact]
    public async Task ToResultAsyncOfT_ErrorResponseWhoseContentCannotBeRead_ReturnsUnreadableContent()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.InternalServerError, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatus].ShouldBe(500);
    }

    [Fact]
    public async Task ToResultAsyncOfString_TextContentThatCannotBeRead_ReturnsUnreadableContent()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."), "text/plain");
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
    }

    #endregion

    #region the exceptions

    // An exception without an HttpRequestError (or with HttpRequestError.Unknown) stores only its type.
    public static TheoryData<Exception, string> ExceptionsWithoutAnHttpRequestError
        => new()
        {
            { new IOException("The connection was reset."), "System.IO.IOException" },
            { new EndOfStreamException("The stream ended."), "System.IO.EndOfStreamException" },
            { new HttpRequestException("The request failed."), "System.Net.Http.HttpRequestException" },
            { new HttpIOException(HttpRequestError.Unknown, "Unknown."), "System.Net.Http.HttpIOException" },
        };

    [Theory]
    [MemberData(nameof(ExceptionsWithoutAnHttpRequestError))]
    public async Task ToResultAsyncOfT_ExceptionWithoutAnHttpRequestError_StoresOnlyItsType(Exception thrown, string expectedType)
    {
        // Arrange
        using var content = new ThrowingHttpContent(thrown);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        result.Error.Metadata.Count.ShouldBe(2);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.ExceptionType);
        result.Error.Metadata[HttpErrorMetadataKeys.ExceptionType].ShouldBe(expectedType);
        result.Error.Metadata.Keys.ShouldNotContain(HttpErrorMetadataKeys.HttpRequestError);
    }

    [Fact]
    public async Task ToResultAsyncOfT_HttpRequestExceptionWithAnHttpRequestError_StoresItsName()
    {
        // Arrange
        var thrown = new HttpRequestException(HttpRequestError.InvalidResponse, "The response is not valid.");
        using var content = new ThrowingHttpContent(thrown);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpRequestError);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpRequestError].ShouldBe("InvalidResponse");
    }

    [Fact]
    public async Task ToResultAsyncOfT_ExceptionWithAMessage_HasTheMessageAsDetail()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Detail.ShouldBe("The connection was reset.");
    }

    // The core rejects a blank detail, so a blank message gives none.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ToResultAsyncOfT_ExceptionWithABlankMessage_HasNoDetail(string message)
    {
        // Arrange
        using var content = new ThrowingHttpContent(new IOException(message));
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        result.Error.Detail.ShouldBeNull();
    }

    // Only a failure of the read is an error; any other exception is a bug or a cancellation. It is thrown as it is:
    // regression (in 2.x, a fault surfaced as an AggregateException).
    [Fact]
    public async Task ToResultAsyncOfT_ContentThrowsAnotherException_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new InvalidOperationException("Not a failure of the read.");
        using var content = new ThrowingHttpContent(thrown);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<Order>(cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    [Fact]
    public async Task ToResultAsync_ErrorContentThrowsAnotherException_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new InvalidOperationException("Not a failure of the read.");
        using var content = new ThrowingHttpContent(thrown);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync(cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    [Fact]
    public async Task ToResultAsyncOfT_ContentThrowsOperationCanceledException_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new OperationCanceledException("The read was canceled.");
        using var content = new ThrowingHttpContent(thrown);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<Order>(cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    [Fact]
    public async Task ToResultAsync_ErrorContentThrowsOperationCanceledException_ThrowsTheSameException()
    {
        // Arrange
        var thrown = new TaskCanceledException("The read was canceled.");
        using var content = new ThrowingHttpContent(thrown);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync(cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeSameAs(thrown);
    }

    #endregion

    #region the functions of the caller

    // There is no body to map, so a function that maps the body is not called.
    [Fact]
    public async Task ToResultAsync_BuilderFunctionAndContentThatCannotBeRead_DoesNotCallTheFunction()
    {
        // Arrange
        var called = false;
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);

        // Act
        var result = await response.ToResultAsync<ApiError>((_, _, _) => called = true, cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ToResultAsync_ErrorFunctionAndContentThatCannotBeRead_DoesNotCallTheFunction()
    {
        // Arrange
        var called = false;
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);

        // Act
        var result = await response.ToResultAsync<ApiError>(
            (_, context) =>
            {
                called = true;

                return Error.NotFound(ErrorUri.None, context.DefaultTitle);
            },
            cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.UnreadableContent.Uri);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ToResultAsync_ConfigureErrorAndContentThatCannotBeRead_RunsWithEmptyContent()
    {
        // Arrange
        HttpErrorContext? received = null;
        var options = new HttpResultOptions
        {
            ConfigureError = (context, builder) =>
            {
                received = context;
                builder.WithMetadata("traceId", "abc");
            }
        };
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.TooManyRequests, content);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        received.ShouldNotBeNull();
        received.Response.ShouldBeSameAs(response);
        received.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        received.Content.ShouldBeEmpty();
        received.DefaultCategory.ShouldBe(ErrorCategory.LimitExceeded);
        received.DefaultTitle.ShouldBe("Too Many Requests");
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.Metadata.Keys.ShouldContain("traceId");
        result.Error.Metadata["traceId"].ShouldBe("abc");
    }

    [Fact]
    public async Task ToResultAsyncOfT_ConfigureErrorAndSuccessContentThatCannotBeRead_RunsWithTheContextOfTheResponse()
    {
        // Arrange
        HttpErrorContext? received = null;
        var mapStatusCalls = 0;
        var options = new HttpResultOptions
        {
            MapStatus = _ =>
            {
                mapStatusCalls++;
                return ErrorCategory.Critical;
            },
            ConfigureError = (context, _) => received = context
        };
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        received.ShouldNotBeNull();
        received.StatusCode.ShouldBe(HttpStatusCode.OK);
        received.Content.ShouldBeEmpty();
        received.DefaultCategory.ShouldBe(ErrorCategory.Failure);
        received.DefaultTitle.ShouldBe("OK");
        mapStatusCalls.ShouldBe(0);
    }

    #endregion
}
