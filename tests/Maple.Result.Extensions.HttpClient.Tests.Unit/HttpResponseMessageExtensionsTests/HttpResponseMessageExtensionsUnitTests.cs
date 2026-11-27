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
/// What every overload of ToResultAsync does: the arguments, the options, the cancellation and the response after the call.
/// </summary>
public class HttpResponseMessageExtensionsUnitTests
{
    private const string ErrorJson = """{"code":"E42","message":"Not here."}""";
    private const string OrderJson = """{"id":42,"customer":"Ada"}""";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region arguments

    public static TheoryData<string, Func<HttpResponseMessage, Task>> Overloads
        => new()
        {
            { "ToResultAsync()", response => response.ToResultAsync(cancellationToken: CancellationToken) },
            { "ToResultAsync<T>()", response => response.ToResultAsync<Order>(cancellationToken: CancellationToken) },
            {
                "ToResultAsync<TError>(Action)",
                response => response.ToResultAsync<ApiError>((_, _, _) => { }, cancellationToken: CancellationToken)
            },
            {
                "ToResultAsync<T, TError>(Action)",
                response => response.ToResultAsync<Order, ApiError>((_, _, _) => { }, cancellationToken: CancellationToken)
            },
            {
                "ToResultAsync<TError>(Func)",
                response => response.ToResultAsync<ApiError>((_, context) => CreateError(context), cancellationToken: CancellationToken)
            },
            {
                "ToResultAsync<T, TError>(Func)",
                response => response.ToResultAsync<Order, ApiError>((_, context) => CreateError(context), cancellationToken: CancellationToken)
            },
        };

    // The arguments are checked when the method is called, not when its task is awaited.
    [Theory]
    [MemberData(nameof(Overloads))]
    public void ToResultAsync_NullResponse_ThrowsArgumentNullException(string overload, Func<HttpResponseMessage, Task> toResultAsync)
    {
        // Act
        var exception = Record.Exception(() => { _ = toResultAsync(null!); });

        // Assert
        var argumentException = exception.ShouldBeOfType<ArgumentNullException>(overload);
        argumentException.ParamName.ShouldBe("response");
    }

    public static TheoryData<string, Func<HttpResponseMessage, Task>> OverloadsWithoutAMapper
        => new()
        {
            {
                "ToResultAsync<TError>(Action)",
                response => response.ToResultAsync((Action<ApiError?, HttpErrorContext, ErrorBuilder>)null!, cancellationToken: CancellationToken)
            },
            {
                "ToResultAsync<T, TError>(Action)",
                response => response.ToResultAsync<Order, ApiError>(
                    (Action<ApiError?, HttpErrorContext, ErrorBuilder>)null!,
                    cancellationToken: CancellationToken)
            },
            {
                "ToResultAsync<TError>(Func)",
                response => response.ToResultAsync((Func<ApiError?, HttpErrorContext, Error>)null!, cancellationToken: CancellationToken)
            },
            {
                "ToResultAsync<T, TError>(Func)",
                response => response.ToResultAsync<Order, ApiError>(
                    (Func<ApiError?, HttpErrorContext, Error>)null!,
                    cancellationToken: CancellationToken)
            },
        };

    [Theory]
    [MemberData(nameof(OverloadsWithoutAMapper))]
    public void ToResultAsync_NullMapError_ThrowsArgumentNullException(string overload, Func<HttpResponseMessage, Task> toResultAsync)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);

        // Act
        var exception = Record.Exception(() => { _ = toResultAsync(response); });

        // Assert
        var argumentException = exception.ShouldBeOfType<ArgumentNullException>(overload);
        argumentException.ParamName.ShouldBe("mapError");
    }

    #endregion

    #region options

    [Fact]
    public async Task ToResultAsync_OptionsArgument_IsUsedInsteadOfTheOptionsOfTheRequest()
    {
        // Arrange
        var argument = new HttpResultOptions { MapStatus = _ => ErrorCategory.Conflict };
        var ofTheRequest = new HttpResultOptions { MapStatus = _ => ErrorCategory.Timeout };
        using var request = new HttpRequestMessage();
        request.Options.Set(HttpResultOptions.RequestOptionsKey, ofTheRequest);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);
        response.RequestMessage = request;

        // Act
        var result = await response.ToResultAsync(argument, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Conflict);
    }

    // A handler of the client can set the options on each request, so that the calls need no argument.
    [Fact]
    public async Task ToResultAsync_NoArgumentAndOptionsOfTheRequest_UsesTheOptionsOfTheRequest()
    {
        // Arrange
        var ofTheRequest = new HttpResultOptions { MapStatus = _ => ErrorCategory.Timeout };
        using var request = new HttpRequestMessage();
        request.Options.Set(HttpResultOptions.RequestOptionsKey, ofTheRequest);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);
        response.RequestMessage = request;

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Timeout);
    }

    [Fact]
    public async Task ToResultAsync_NoArgumentAndNoOptionsOfTheRequest_UsesTheDefaultOptions()
    {
        // Arrange
        using var request = new HttpRequestMessage();
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound);
        response.RequestMessage = request;

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.NotFound);
    }

    // As JsonSerializerOptions: an instance that was used cannot be changed, so a later change cannot go unnoticed by calls that share it.
    [Fact]
    public async Task ToResultAsync_Options_AreReadOnlyAfterTheCall()
    {
        // Arrange
        var options = new HttpResultOptions();
        using var response = ResponseFactory.Create(HttpStatusCode.OK);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var exception = Record.Exception(() => options.MapStatus = _ => null);
        exception.ShouldBeOfType<InvalidOperationException>();
    }

    #endregion

    #region response

    // The caller owns the response: it is not disposed, and its headers and buffered content can still be read.
    [Fact]
    public async Task ToResultAsync_ErrorResponse_IsNotDisposed()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        var content = await response.Content.ReadAsStringAsync(CancellationToken);
        content.ShouldBe(ErrorJson);
    }

    [Fact]
    public async Task ToResultAsyncOfT_SuccessResponse_IsNotDisposed()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var content = await response.Content.ReadAsStringAsync(CancellationToken);
        content.ShouldBe(OrderJson);
    }

    // Using a response after disposing it is a bug of the caller, so it is not turned into an error.
    [Fact]
    public async Task ToResultAsync_DisposedErrorResponse_ThrowsObjectDisposedException()
    {
        // Arrange
        var response = ResponseFactory.CreateJson(HttpStatusCode.NotFound, ErrorJson);
        response.Dispose();

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync(cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async Task ToResultAsyncOfT_DisposedSuccessResponse_ThrowsObjectDisposedException()
    {
        // Arrange
        var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);
        response.Dispose();

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<Order>(cancellationToken: CancellationToken));

        // Assert
        exception.ShouldBeOfType<ObjectDisposedException>();
    }

    #endregion

    #region cancellation

    // Cancellation is never turned into an error.
    [Fact]
    public async Task ToResultAsync_CanceledTokenAndErrorResponse_ThrowsOperationCanceledException()
    {
        // Arrange
        using var content = new CancellationObservingHttpContent(ErrorJson);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync(cancellationToken: cancellation.Token));

        // Assert
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task ToResultAsyncOfT_CanceledTokenAndSuccessResponse_ThrowsOperationCanceledException()
    {
        // Arrange
        using var content = new CancellationObservingHttpContent(OrderJson);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<Order>(cancellationToken: cancellation.Token));

        // Assert
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task ToResultAsyncOfT_CanceledTokenAndTextContent_ThrowsOperationCanceledException()
    {
        // Arrange
        using var content = new CancellationObservingHttpContent("Hello", "text/plain");
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<string>(cancellationToken: cancellation.Token));

        // Assert
        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task ToResultAsync_TokenThatIsNotCanceled_ReadsTheContent()
    {
        // Arrange
        using var content = new CancellationObservingHttpContent(OrderJson);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    #endregion

    #region synchronization context

    // A library must not continue on the context of its caller: on a user interface thread that would run its code there,
    // and deadlock when the caller blocks on the task.
    [Fact]
    public async Task ToResultAsyncOfT_JsonContentAndASynchronizationContext_DoesNotContinueOnTheContext()
    {
        // Arrange
        using var content = new AsynchronousHttpContent(OrderJson);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);
        var context = new CountingSynchronizationContext();

        // Act
        var result = await StartOnContext(context, () => response.ToResultAsync<Order>(cancellationToken: CancellationToken));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        context.Posts.ShouldBe(0);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/plain; charset=utf8")]
    public async Task ToResultAsyncOfString_TextContentAndASynchronizationContext_DoesNotContinueOnTheContext(string contentType)
    {
        // Arrange
        using var content = new AsynchronousHttpContent("Hello", contentType);
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);
        var context = new CountingSynchronizationContext();

        // Act
        var result = await StartOnContext(context, () => response.ToResultAsync<string>(cancellationToken: CancellationToken));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        context.Posts.ShouldBe(0);
    }

    [Fact]
    public async Task ToResultAsync_ErrorResponseAndASynchronizationContext_DoesNotContinueOnTheContext()
    {
        // Arrange
        using var content = new AsynchronousHttpContent(ErrorJson);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);
        var context = new CountingSynchronizationContext();

        // Act
        var result = await StartOnContext(context, () => response.ToResultAsync(cancellationToken: CancellationToken));

        // Assert
        result.IsError.ShouldBeTrue();
        context.Posts.ShouldBe(0);
    }

    [Fact]
    public async Task ToResultAsyncOfT_ErrorResponseAndASynchronizationContext_DoesNotContinueOnTheContext()
    {
        // Arrange
        using var content = new AsynchronousHttpContent(ErrorJson);
        using var response = ResponseFactory.Create(HttpStatusCode.NotFound, content);
        var context = new CountingSynchronizationContext();

        // Act
        var result = await StartOnContext(context, () => response.ToResultAsync<Order>(cancellationToken: CancellationToken));

        // Assert
        result.IsError.ShouldBeTrue();
        context.Posts.ShouldBe(0);
    }

    #endregion

    #region helper methods

    private static Error CreateError(HttpErrorContext context)
    {
        return Error.Failure(ErrorUri.None, context.DefaultTitle);
    }

    /// <summary>
    /// Starts the call with the given synchronization context as the current one, as code of a user interface thread does;
    /// the context is restored before the task is awaited.
    /// </summary>
    private static Task<TResult> StartOnContext<TResult>(SynchronizationContext context, Func<Task<TResult>> start)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);

        try
        {
            return start();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    #endregion
}
