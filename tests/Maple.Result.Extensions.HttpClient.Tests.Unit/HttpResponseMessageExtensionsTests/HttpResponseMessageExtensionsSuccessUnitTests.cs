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
using System.Text;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.HttpResponseMessageExtensionsTests;

/// <summary>
/// A response with a success status code: Result ignores the body, and Result&lt;T&gt; reads it as T.
/// </summary>
public class HttpResponseMessageExtensionsSuccessUnitTests
{
    private const string OrderJson = """{"id":42,"customer":"Ada"}""";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region Result

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData((HttpStatusCode)299)]
    public async Task ToResultAsync_SuccessStatusCode_ReturnsSuccess(HttpStatusCode statusCode)
    {
        // Arrange
        using var response = ResponseFactory.Create(statusCode);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_SuccessWithABody_IgnoresTheBody()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, "this is not JSON", "application/json");

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // The body is not even read, so a connection that drops after the headers does not matter.
    [Fact]
    public async Task ToResultAsync_SuccessWithContentThatCannotBeRead_ReturnsSuccess()
    {
        // Arrange
        using var content = new ThrowingHttpContent(new IOException("The connection was reset."));
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ToResultAsync_Success_DoesNotCallMapStatusOrConfigureError()
    {
        // Arrange
        var calls = 0;
        var options = new HttpResultOptions
        {
            MapStatus = _ =>
            {
                calls++;
                return null;
            },
            ConfigureError = (_, _) => calls++
        };
        using var response = ResponseFactory.Create(HttpStatusCode.OK);

        // Act
        var result = await response.ToResultAsync(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        calls.ShouldBe(0);
    }

    #endregion

    #region Result<T>: JSON

    [Fact]
    public async Task ToResultAsyncOfT_JsonObject_ReturnsTheValue()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    [Fact]
    public async Task ToResultAsyncOfT_JsonNumber_ReturnsTheValue()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "42");

        // Act
        var result = await response.ToResultAsync<int>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task ToResultAsyncOfT_JsonBoolean_ReturnsTheValue()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "false");

        // Act
        var result = await response.ToResultAsync<bool>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task ToResultAsyncOfT_JsonArray_ReturnsTheValue()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "[1,2,3]");

        // Act
        var result = await response.ToResultAsync<int[]>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ToResultAsyncOfT_NullableValueTypeWithAValue_ReturnsTheValue()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "5");

        // Act
        var result = await response.ToResultAsync<int?>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(5);
    }

    // The default options are JsonSerializerOptions.Web: camel case names, read ignoring the case.
    [Fact]
    public async Task ToResultAsyncOfT_DefaultOptions_ReadNamesIgnoringCase()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, """{"ID":42,"Customer":"Ada"}""");

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    [Fact]
    public async Task ToResultAsyncOfT_JsonSerializerOptionsOfTheOptions_AreUsed()
    {
        // Arrange
        var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var options = new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, """{"order_id":7}""");

        // Act
        var result = await response.ToResultAsync<Shipment>(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Shipment(7));
    }

    // JSON without a Content-Type header, or with another media type, is still read as JSON: only the status code decides.
    [Theory]
    [InlineData(null)]
    [InlineData("application/octet-stream")]
    [InlineData("application/vnd.acme.order+json")]
    [InlineData("text/plain")]
    public async Task ToResultAsyncOfT_AnyMediaType_ReadsTheBodyAsJson(string? contentType)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, OrderJson, contentType);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    #endregion

    #region Result<T>: character sets

    [Fact]
    public async Task ToResultAsyncOfT_JsonInAnotherCharacterSet_ReturnsTheValue()
    {
        // Arrange
        var body = Encoding.Unicode.GetBytes("""{"id":42,"customer":"Zoë"}""");
        using var response = ResponseFactory.Create(HttpStatusCode.OK, body, "application/json; charset=utf-16");

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Zoë"));
    }

    [Theory]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("application/json; charset=\"utf-8\"")]
    [InlineData("application/json; charset=UTF-8")]
    public async Task ToResultAsyncOfT_JsonInUtf8_ReturnsTheValue(string contentType)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, """{"id":42,"customer":"Zoë"}""", contentType);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Zoë"));
    }

    // A character set that the runtime does not know (the misspelt "utf8", or windows-1252 without its provider) must not throw;
    // JSON is UTF-8 unless stated otherwise (RFC 8259).
    [Theory]
    [InlineData("application/json; charset=utf8")]
    [InlineData("application/json; charset=windows-1252")]
    [InlineData("application/json; charset=bogus")]
    public async Task ToResultAsyncOfT_JsonWithAnUnknownCharacterSet_IsReadAsUtf8(string contentType)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, """{"id":42,"customer":"Zoë"}""", contentType);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Zoë"));
    }

    [Fact]
    public async Task ToResultAsyncOfT_JsonWithAByteOrderMark_ReturnsTheValue()
    {
        // Arrange
        byte[] body = [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(OrderJson)];
        using var response = ResponseFactory.Create(HttpStatusCode.OK, body, "application/json");

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    #endregion

    #region Result<string>

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/plain; charset=utf-8")]
    [InlineData("text/html")]
    [InlineData("TEXT/CSV")]
    public async Task ToResultAsyncOfString_TextBody_ReturnsTheRawText(string contentType)
    {
        // Arrange
        var text = "He said \"hello\", twice.";
        using var response = ResponseFactory.Create(HttpStatusCode.OK, text, contentType);

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(text);
    }

    [Fact]
    public async Task ToResultAsyncOfString_TextBodyWithAnUnknownCharacterSet_IsReadAsUtf8()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, "Zoë", "text/plain; charset=utf8");

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Zoë");
    }

    [Fact]
    public async Task ToResultAsyncOfString_TextBodyWithAByteOrderMarkAndAnUnknownCharacterSet_ReturnsTheTextWithoutTheMark()
    {
        // Arrange
        byte[] body = [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes("Zoë")];
        using var response = ResponseFactory.Create(HttpStatusCode.OK, body, "text/plain; charset=utf8");

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Zoë");
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData(null)]
    public async Task ToResultAsyncOfString_JsonString_ReturnsTheTextOfTheJsonString(string? contentType)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, "\"He said \\\"hello\\\".\"", contentType);

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("He said \"hello\".");
    }

    [Fact]
    public async Task ToResultAsyncOfString_JsonBodyThatIsNotAString_ReturnsInvalidSuccessContent()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
    }

    [Fact]
    public async Task ToResultAsyncOfString_EmptyTextBody_ReturnsInvalidSuccessContent()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.OK, "", "text/plain");

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
        result.Error.Detail.ShouldBe("The response has no content.");
    }

    // The body of a chunked response: that it is empty is known only after it is read.
    [Fact]
    public async Task ToResultAsyncOfString_EmptyTextBodyOfUnknownLength_ReturnsInvalidSuccessContent()
    {
        // Arrange
        using var content = new UnknownLengthHttpContent([], "text/plain");
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<string>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
        result.Error.Detail.ShouldBe("The response has no content.");
    }

    #endregion

    #region Result<T>: content that is not a value

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task ToResultAsyncOfT_NoContent_ReturnsInvalidSuccessContent(HttpStatusCode statusCode)
    {
        // Arrange
        using var response = ResponseFactory.Create(statusCode);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Failure);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
        result.Error.Title.ShouldBe(HttpClientErrors.InvalidSuccessContent.Title);
        result.Error.Detail.ShouldBe("The response has no content.");
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    // No exception is thrown for an empty body, so there is no type of an exception to store.
    [Fact]
    public async Task ToResultAsyncOfT_NoContent_StoresOnlyTheStatusCode()
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.NoContent);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(1);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(204);
    }

    // The body of a chunked response: that it is empty is known only after it is read, so the serializer reports it.
    [Fact]
    public async Task ToResultAsyncOfT_EmptyBodyOfUnknownLength_ReturnsInvalidSuccessContent()
    {
        // Arrange
        using var content = new UnknownLengthHttpContent([], "application/json");
        using var response = ResponseFactory.Create(HttpStatusCode.OK, content);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Failure);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.ExceptionType);
    }

    [Theory]
    [InlineData("null")]
    [InlineData(" null ")]
    public async Task ToResultAsyncOfT_JsonNull_ReturnsInvalidSuccessContent(string json)
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, json);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Failure);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
        result.Error.Detail.ShouldBe("The content of the response is the JSON value ‘null’.");
        result.Error.Metadata.Keys.ShouldNotContain(HttpErrorMetadataKeys.ExceptionType);
    }

    // A successful result has a value, also when T is nullable.
    [Fact]
    public async Task ToResultAsyncOfT_JsonNullForANullableValueType_ReturnsInvalidSuccessContent()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "null");

        // Act
        var result = await response.ToResultAsync<int?>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
    }

    [Theory]
    [InlineData("""{"id":""")]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    [InlineData("   ")]
    [InlineData("<html><body>Bad Gateway</body></html>")]
    public async Task ToResultAsyncOfT_BodyThatIsNotAValueOfT_ReturnsInvalidSuccessContent(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, body);

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Failure);
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
        result.Error.Title.ShouldBe(HttpClientErrors.InvalidSuccessContent.Title);
    }

    [Fact]
    public async Task ToResultAsyncOfT_BodyThatIsNotAValueOfT_StoresTheTypeOfTheExceptionAndTheStatusCode()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.Created, "[1,2]");

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Count.ShouldBe(2);
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.ExceptionType);
        result.Error.Metadata[HttpErrorMetadataKeys.ExceptionType].ShouldBe("System.Text.Json.JsonException");
        result.Error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        result.Error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(201);
    }

    // The message of the JsonException names the path and the position, not the content.
    [Fact]
    public async Task ToResultAsyncOfT_BodyThatIsNotAValueOfT_HasTheMessageOfTheExceptionAsDetail()
    {
        // Arrange
        var body = "[1,2]";
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, body);
        var expected = Record.Exception(() => JsonSerializer.Deserialize<Order>(body, JsonSerializerOptions.Web));

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        expected.ShouldNotBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.Detail.ShouldBe(expected.Message);
    }

    // 2.x read an enum name without quotes; it is not JSON.
    [Fact]
    public async Task ToResultAsyncOfT_EnumNameWithoutQuotes_ReturnsInvalidSuccessContent()
    {
        // Arrange
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "Shipped");

        // Act
        var result = await response.ToResultAsync<ShipmentStatus>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(HttpClientErrors.InvalidSuccessContent.Uri);
    }

    [Fact]
    public async Task ToResultAsyncOfT_InvalidContent_RunsConfigureErrorWithTheContextOfTheResponse()
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
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "[1,2]");

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        received.ShouldNotBeNull();
        received.Response.ShouldBeSameAs(response);
        received.StatusCode.ShouldBe(HttpStatusCode.OK);
        received.Content.ShouldBeEmpty();
        received.DefaultCategory.ShouldBe(ErrorCategory.Failure);
        received.DefaultTitle.ShouldBe("OK");
        result.Error.ShouldNotBeNull();
        result.Error.Metadata.Keys.ShouldContain("traceId");
        result.Error.Metadata["traceId"].ShouldBe("abc");
    }

    // A success status code never reaches the status map.
    [Fact]
    public async Task ToResultAsyncOfT_InvalidContent_DoesNotCallMapStatus()
    {
        // Arrange
        var calls = 0;
        var options = new HttpResultOptions
        {
            MapStatus = _ =>
            {
                calls++;
                return ErrorCategory.Critical;
            }
        };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, "[1,2]");

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Failure);
        calls.ShouldBe(0);
    }

    [Fact]
    public async Task ToResultAsyncOfT_Value_DoesNotCallMapStatusOrConfigureError()
    {
        // Arrange
        var calls = 0;
        var options = new HttpResultOptions
        {
            MapStatus = _ =>
            {
                calls++;
                return null;
            },
            ConfigureError = (_, _) => calls++
        };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        calls.ShouldBe(0);
    }

    #endregion

    #region Result<T>: JsonSerializerOptions

    // Regression (2.x changed the options that it was given, which fails for read-only ones such as JsonSerializerOptions.Web).
    [Fact]
    public async Task ToResultAsyncOfT_ReadOnlyJsonSerializerOptions_WorkAndStayUnchanged()
    {
        // Arrange
        var resolver = JsonSerializerOptions.Web.TypeInfoResolver;
        var converters = JsonSerializerOptions.Web.Converters.Count;
        var options = new HttpResultOptions { JsonSerializerOptions = JsonSerializerOptions.Web };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
        JsonSerializerOptions.Web.TypeInfoResolver.ShouldBeSameAs(resolver);
        JsonSerializerOptions.Web.Converters.Count.ShouldBe(converters);
    }

    // new JsonSerializerOptions { … } has no resolver until it is first used; it must work as it does with the JsonSerializer.
    [Fact]
    public async Task ToResultAsyncOfT_JsonSerializerOptionsWithoutAResolver_Work()
    {
        // Arrange
        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var options = new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
        jsonSerializerOptions.IsReadOnly.ShouldBeTrue();
    }

    // As in a trimmed or Native AOT application: the metadata comes from a source-generated context.
    [Fact]
    public async Task ToResultAsyncOfT_SourceGeneratedMetadataOfT_ReturnsTheValue()
    {
        // Arrange
        var options = CreateSourceGeneratedOptions();
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, OrderJson);

        // Act
        var result = await response.ToResultAsync<Order>(options, CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Order(42, "Ada"));
    }

    // A type that the context does not have is a mistake in the setup of the application, not in the response: it is not turned into an error.
    [Fact]
    public async Task ToResultAsyncOfT_NoMetadataOfT_ThrowsNotSupportedException()
    {
        // Arrange
        var options = CreateSourceGeneratedOptions();
        using var response = ResponseFactory.CreateJson(HttpStatusCode.OK, """{"orderId":7}""");

        // Act
        var exception = await Record.ExceptionAsync(() => response.ToResultAsync<Shipment>(options, CancellationToken));

        // Assert
        var notSupportedException = exception.ShouldBeOfType<NotSupportedException>();
        notSupportedException.Message.ShouldContain(nameof(Shipment));
    }

    #endregion

    #region helper methods

    private static HttpResultOptions CreateSourceGeneratedOptions()
    {
        var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = TestJsonContext.Default };

        return new HttpResultOptions { JsonSerializerOptions = jsonSerializerOptions };
    }

    #endregion

    private sealed record Shipment(int OrderId);

    private enum ShipmentStatus
    {
        Pending,
        Shipped
    }
}
