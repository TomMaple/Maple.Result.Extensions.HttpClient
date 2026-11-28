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
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.HttpResponseMessageExtensionsTests;

/// <summary>
/// An error response with an RFC 9457 problem details body, mapped without a function of the caller: the body is read as
/// <i>Reading problem details</i> of docs/problem-details.md of the core describes.
/// </summary>
public class HttpResponseMessageExtensionsProblemDetailsUnitTests
{
    private const string RequestUri = "https://api.example.com/orders/42";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    #region when the body is read

    [Theory]
    [InlineData("application/problem+json")]
    [InlineData("application/problem+json; charset=utf-8")]
    [InlineData("APPLICATION/PROBLEM+JSON")]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("application/vnd.api+json")]
    [InlineData("application/merge-patch+json")]
    public async Task ToResultAsync_BodyInAJsonMediaType_ReadsTheProblemDetails(string contentType)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.Forbidden, """{"title":"You do not have enough credit."}""", contentType);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe("You do not have enough credit.");
    }

    // An error page of a proxy or a server is not parsed: the error of the status code is returned, and the text is only in Content.
    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("text/json")]
    [InlineData("application/xml")]
    [InlineData("application/problem+xml")]
    [InlineData("application/octet-stream")]
    public async Task ToResultAsync_BodyInAnotherMediaType_ReturnsTheErrorOfTheStatusCode(string contentType)
    {
        // Arrange
        const string Body = """{"title":"You do not have enough credit.","traceId":"abc"}""";
        string? content = null;
        var options = new HttpResultOptions { ConfigureError = (context, _) => content = context.Content };
        using var response = ResponseFactory.Create(HttpStatusCode.Forbidden, Body, contentType);

        // Act
        var error = await ReadErrorAsync(response, options);

        // Assert
        error.Title.ShouldBe("Forbidden");
        error.Metadata.Keys.ShouldNotContain("traceId");
        content.ShouldBe(Body);
    }

    [Theory]
    [InlineData("""{"title":"You do not have enough credit."}""")]
    [InlineData(""" {"title":"You do not have enough credit."}""")]
    [InlineData("\r\n\t{\"title\":\"You do not have enough credit.\"}")]
    public async Task ToResultAsync_ObjectWithoutAContentType_ReadsTheProblemDetails(string body)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.Forbidden, body, null);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe("You do not have enough credit.");
    }

    [Theory]
    [InlineData("You do not have enough credit.")]
    [InlineData("""[{"title":"You do not have enough credit."}]""")]
    [InlineData("<html><body>Forbidden</body></html>")]
    public async Task ToResultAsync_OtherBodyWithoutAContentType_ReturnsTheErrorOfTheStatusCode(string body)
    {
        // Arrange
        using var response = ResponseFactory.Create(HttpStatusCode.Forbidden, body, null);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe("Forbidden");
        error.Detail.ShouldBeNull();
    }

    // A body that is not a JSON object is not an error by itself, and nothing throws.
    [Theory]
    [InlineData("")]
    [InlineData("""{"title":"You do not have enough credit." """)]
    [InlineData("""{"title":"You do not have enough credit.",}""")]
    [InlineData("""{"title":"You do not have enough credit."} {"detail":"x"}""")]
    [InlineData("{'title':'You do not have enough credit.'}")]
    [InlineData("[]")]
    [InlineData("\"You do not have enough credit.\"")]
    [InlineData("42")]
    [InlineData("null")]
    public async Task ToResultAsync_BodyThatIsNotAJsonObject_ReturnsTheErrorOfTheStatusCode(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Category.ShouldBe(ErrorCategory.PermissionDenied);
        error.TypeUri.ShouldBe(ErrorUri.None);
        error.Title.ShouldBe("Forbidden");
        error.Detail.ShouldBeNull();
        error.ErrorDetails.ShouldBeEmpty();
        error.Metadata.Count.ShouldBe(1);
    }

    // A document nested deeper than the reader allows is not read.
    [Fact]
    public async Task ToResultAsync_BodyNestedTooDeeply_ReturnsTheErrorOfTheStatusCode()
    {
        // Arrange
        var nested = new string('[', 100) + new string(']', 100);
        var body = $$"""{"title":"You do not have enough credit.","nested":{{nested}}}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe("Forbidden");
        error.Metadata.Keys.ShouldNotContain("nested");
    }

    [Fact]
    public async Task ToResultAsyncOfT_ProblemDetails_ReadsTheProblemDetails()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"title":"You do not have enough credit.","detail":"It costs 50."}""");

        // Act
        var result = await response.ToResultAsync<Order>(cancellationToken: CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe("You do not have enough credit.");
        result.Error.Detail.ShouldBe("It costs 50.");
    }

    // A function of the caller reads the body itself: it gets the builder of the status code, not the problem details.
    [Fact]
    public async Task ToResultAsync_FunctionOfTheCaller_DoesNotReadTheProblemDetails()
    {
        // Arrange
        string? title = null;
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"title":"You do not have enough credit.","traceId":"abc"}""");

        // Act
        var result = await response.ToResultAsync<ApiError>((_, _, builder) => title = builder.Build().Title, cancellationToken: CancellationToken);

        // Assert
        result.Error.ShouldNotBeNull();
        title.ShouldBe("Forbidden");
        result.Error.Title.ShouldBe("Forbidden");
        result.Error.Metadata.Keys.ShouldNotContain("traceId");
    }

    #endregion

    #region status

    // The status member is advisory (RFC 9457, section 3.1.2): the status code of the response decides the category.
    [Fact]
    public async Task ToResultAsync_StatusMember_IsNotRead()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.NotFound, """{"title":"The order is not here.","status":400}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Category.ShouldBe(ErrorCategory.NotFound);
        error.Metadata.Keys.ShouldNotContain("status");
        error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
    }

    #endregion

    #region title

    [Fact]
    public async Task ToResultAsync_Title_IsTheTitleOfTheError()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"title":"You do not have enough credit."}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe("You do not have enough credit.");
    }

    [Fact]
    public async Task ToResultAsync_TitleWithWhiteSpaceAtTheEnds_IsKeptAsSent()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"title":" You do not have enough credit. "}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe(" You do not have enough credit. ");
    }

    // RFC 9457, section 3.1.3: the title of the status code.
    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"title":""}""")]
    [InlineData("""{"title":"   "}""")]
    [InlineData("""{"title":null}""")]
    [InlineData("""{"title":42}""")]
    [InlineData("""{"title":["You do not have enough credit."]}""")]
    [InlineData("""{"Title":"You do not have enough credit."}""")]
    public async Task ToResultAsync_MissingOrBlankTitle_IsThePhraseOfTheStatusCode(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe("Forbidden");
    }

    // JSON does not forbid a repeated member (RFC 8259, section 4); the last one is read, as by most parsers.
    [Theory]
    [InlineData("""{"title":"First.","title":"Second."}""", "Second.")]
    [InlineData("""{"title":"First.","title":""}""", "Forbidden")]
    public async Task ToResultAsync_RepeatedTitle_ReadsTheLastOne(string body, string expected)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Title.ShouldBe(expected);
    }

    #endregion

    #region type

    [Theory]
    [InlineData("https://example.com/probs/out-of-credit")]
    [InlineData("tag:example.com,2026:out-of-credit")]
    [InlineData("urn:example:out-of-credit")]
    [InlineData("/probs/out-of-credit")]
    [InlineData("mailto:support@example.com")]
    public async Task ToResultAsync_Type_IsTheTypeOfTheError(string type)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, $$"""{"type":"{{type}}"}""", RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.Parse(type));
        error.TypeUri.Value.ShouldBe(type);
    }

    [Theory]
    [InlineData("about:blank")]
    [InlineData("ABOUT:BLANK")]
    [InlineData("About:Blank")]
    public async Task ToResultAsync_AboutBlankInAnyCase_IsTheNoneType(string type)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, $$"""{"type":"{{type}}"}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.None);
        error.TypeUri.Value.ShouldBe("about:blank");
    }

    // RFC 9457, section 3.1.1: a missing type is about:blank.
    [Theory]
    [InlineData("""{"title":"You do not have enough credit."}""")]
    [InlineData("""{"type":null}""")]
    [InlineData("""{"type":42}""")]
    [InlineData("""{"type":""}""")]
    [InlineData("""{"type":"   "}""")]
    public async Task ToResultAsync_MissingType_IsTheNoneType(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body, RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.None);
    }

    // RFC 3986, section 5: a relative reference is resolved against the URI of the request. Regression (2.x read it as about:blank).
    [Theory]
    [InlineData("out-of-credit", "https://api.example.com/orders/out-of-credit")]
    [InlineData("./out-of-credit", "https://api.example.com/orders/out-of-credit")]
    [InlineData("../probs/out-of-credit", "https://api.example.com/probs/out-of-credit")]
    [InlineData("probs/out-of-credit?lang=en", "https://api.example.com/orders/probs/out-of-credit?lang=en")]
    [InlineData("out%20of%20credit", "https://api.example.com/orders/out%20of%20credit")]
    [InlineData("?lang=en", "https://api.example.com/orders/42?lang=en")]
    [InlineData("#out-of-credit", "https://api.example.com/orders/42#out-of-credit")]
    public async Task ToResultAsync_RelativeType_IsResolvedAgainstTheRequestUri(string type, string expected)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, $$"""{"type":"{{type}}"}""", RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.Locator(expected));
    }

    // Without an absolute URI of the request, a relative reference cannot be resolved.
    [Fact]
    public async Task ToResultAsync_RelativeTypeWithoutARequest_IsTheNoneType()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"type":"out-of-credit"}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.None);
    }

    [Fact]
    public async Task ToResultAsync_RelativeTypeAndARelativeRequestUri_IsTheNoneType()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"type":"out-of-credit"}""");
        response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("/orders/42", UriKind.Relative));

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.None);
    }

    // A value that is not a URI reference is not resolved, and gives about:blank.
    [Theory]
    [InlineData("out of credit")]
    [InlineData("out-of-credit#a#b")]
    [InlineData("out-of-credit%2")]
    [InlineData("out-of-credit%zz")]
    [InlineData("out-of-crédit")]
    [InlineData("out|of|credit")]
    [InlineData("https://exa mple.com/probs/out-of-credit")]
    [InlineData("tag:example.com:out-of-credit")]
    [InlineData("//example.com/probs/out-of-credit")]
    [InlineData("http://")]
    public async Task ToResultAsync_InvalidType_IsTheNoneType(string type)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, $$"""{"type":"{{type}}"}""", RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.TypeUri.ShouldBe(ErrorUri.None);
    }

    #endregion

    #region instance

    [Theory]
    [InlineData("/orders/42/errors/7")]
    [InlineData("https://api.example.com/errors/7")]
    [InlineData("urn:uuid:0af76519-16cd-43dd-8448-eb211c80319c")]
    public async Task ToResultAsync_Instance_IsTheInstanceOfTheError(string instance)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, $$"""{"instance":"{{instance}}"}""", RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.InstanceUri.ShouldBe(ErrorUri.Parse(instance));
    }

    [Fact]
    public async Task ToResultAsync_RelativeInstance_IsResolvedAgainstTheRequestUri()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"instance":"errors/7"}""", RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.InstanceUri.ShouldBe(ErrorUri.Locator("https://api.example.com/orders/errors/7"));
    }

    [Theory]
    [InlineData("""{"title":"You do not have enough credit."}""")]
    [InlineData("""{"instance":null}""")]
    [InlineData("""{"instance":7}""")]
    [InlineData("""{"instance":""}""")]
    [InlineData("""{"instance":"errors 7"}""")]
    [InlineData("""{"instance":"//example.com/errors/7"}""")]
    public async Task ToResultAsync_MissingOrInvalidInstance_IsNull(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body, RequestUri);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.InstanceUri.ShouldBeNull();
    }

    #endregion

    #region detail

    [Fact]
    public async Task ToResultAsync_Detail_IsKeptAsSent()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"detail":" Your balance is 30, but that costs 50.\n"}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Detail.ShouldBe(" Your balance is 30, but that costs 50.\n");
    }

    // The core rejects a blank detail.
    [Theory]
    [InlineData("""{"title":"You do not have enough credit."}""")]
    [InlineData("""{"detail":""}""")]
    [InlineData("""{"detail":" \t "}""")]
    [InlineData("""{"detail":null}""")]
    [InlineData("""{"detail":{"text":"Your balance is 30."}}""")]
    public async Task ToResultAsync_MissingOrBlankDetail_IsNull(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Detail.ShouldBeNull();
    }

    #endregion

    #region errors (the array of RFC 9457)

    [Fact]
    public async Task ToResultAsync_ErrorsArray_AreTheErrorDetails()
    {
        // Arrange
        const string Body = """
            {
              "errors": [
                { "pointer": "/email", "detail": "The email address is required." },
                { "pointer": "#/age", "detail": "The age must be at least 18.", "detailTemplated": { "templateId": "signup.age.min", "params": { "min": 18 } } },
                { "detail": "The start date is after the end date." }
              ]
            }
            """;

        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, Body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe(
        [
            new ErrorDetail("/email", "The email address is required."),
            new ErrorDetail("#/age", "The age must be at least 18.", new TemplatedMessage("signup.age.min", ("min", 18))),
            new ErrorDetail(null, "The start date is after the end date.")
        ]);
    }

    // An item without a usable detail is dropped; the other items are kept.
    [Theory]
    [InlineData("""{ "pointer": "/email" }""")]
    [InlineData("""{ "pointer": "/email", "detail": "" }""")]
    [InlineData("""{ "pointer": "/email", "detail": "  " }""")]
    [InlineData("""{ "pointer": "/email", "detail": null }""")]
    [InlineData("""{ "pointer": "/email", "detail": 42 }""")]
    [InlineData("""{ "pointer": "/email", "Detail": "The email address is required." }""")]
    [InlineData("\"The email address is required.\"")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task ToResultAsync_ErrorsItemWithoutADetail_IsDropped(string item)
    {
        // Arrange
        var body = $$"""{"errors":[{{item}},{"pointer":"/age","detail":"The age must be at least 18."}]}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe([new ErrorDetail("/age", "The age must be at least 18.")]);
    }

    // RFC 6901, section 7: the core rejects an invalid pointer, so it is dropped and the detail is kept.
    [Theory]
    [InlineData("\"email\"")]
    [InlineData("\"Items[0].Name\"")]
    [InlineData("\"/a~2b\"")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task ToResultAsync_ErrorsItemWithAnInvalidPointer_KeepsTheDetailWithoutAPointer(string pointer)
    {
        // Arrange
        var body = $$"""{"errors":[{"pointer":{{pointer}},"detail":"The email address is required."}]}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe([new ErrorDetail(null, "The email address is required.")]);
    }

    // The pointers of the array are pointers already: they are never changed, not even by ErrorKeyNamingPolicy.
    [Fact]
    public async Task ToResultAsync_ErrorsArrayAndANamingPolicy_KeepsThePointers()
    {
        // Arrange
        var options = new HttpResultOptions { ErrorKeyNamingPolicy = JsonNamingPolicy.CamelCase };
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, """{"errors":[{"pointer":"/Email","detail":"The email address is required."}]}""");

        // Act
        var error = await ReadErrorAsync(response, options);

        // Assert
        error.ErrorDetails.ShouldBe([new ErrorDetail("/Email", "The email address is required.")]);
    }

    [Fact]
    public async Task ToResultAsync_ErrorsItemWithAnInvalidTemplate_KeepsTheDetailWithoutATemplate()
    {
        // Arrange
        const string Body = """{"errors":[{"pointer":"/age","detail":"The age must be at least 18.","detailTemplated":{"templateId":" "}}]}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, Body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe([new ErrorDetail("/age", "The age must be at least 18.")]);
    }

    [Theory]
    [InlineData("""{"errors":[]}""")]
    [InlineData("""{"errors":{}}""")]
    [InlineData("""{"errors":null}""")]
    [InlineData("""{"errors":"The email address is required."}""")]
    [InlineData("""{"errors":42}""")]
    [InlineData("""{"Errors":[{"pointer":"/email","detail":"The email address is required."}]}""")]
    public async Task ToResultAsync_NoErrors_HasNoErrorDetails(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBeEmpty();
    }

    #endregion

    #region errors (the object of ASP.NET Core)

    // ValidationProblemDetails of ASP.NET Core: one error detail per message. Regression (2.x kept the key as the pointer).
    [Fact]
    public async Task ToResultAsync_ErrorsObject_AreTheErrorDetailsWithPointersFromTheKeys()
    {
        // Arrange
        const string Body = """
            {
              "errors": {
                "Email": [ "The Email field is required.", "The Email field is not a valid e-mail address." ],
                "Items[0].Name": [ "The Name field is required." ],
                "$.items[1].quantity": [ "The JSON value could not be converted to System.Int32." ],
                "": [ "A non-empty request Body is required." ]
              }
            }
            """;

        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, Body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe(
        [
            new ErrorDetail("/Email", "The Email field is required."),
            new ErrorDetail("/Email", "The Email field is not a valid e-mail address."),
            new ErrorDetail("/Items/0/Name", "The Name field is required."),
            new ErrorDetail("/items/1/quantity", "The JSON value could not be converted to System.Int32."),
            new ErrorDetail("", "A non-empty request Body is required.")
        ]);
    }

    [Fact]
    public async Task ToResultAsync_ErrorsObjectAndANamingPolicy_ConvertsTheNamesOfThePointers()
    {
        // Arrange
        var options = new HttpResultOptions { ErrorKeyNamingPolicy = JsonNamingPolicy.CamelCase };
        const string Body = """{"errors":{"Email":["The Email field is required."],"Items[0].UnitPrice":["The UnitPrice field is required."]}}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, Body);

        // Act
        var error = await ReadErrorAsync(response, options);

        // Assert
        error.ErrorDetails.ShouldBe(
        [
            new ErrorDetail("/email", "The Email field is required."),
            new ErrorDetail("/items/0/unitPrice", "The UnitPrice field is required.")
        ]);
    }

    // A single message instead of an array, as some services write it.
    [Fact]
    public async Task ToResultAsync_ErrorsObjectWithAMessage_IsOneErrorDetail()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, """{"errors":{"Email":"The Email field is required."}}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe([new ErrorDetail("/Email", "The Email field is required.")]);
    }

    // A value that is not a message is dropped; the other messages are kept.
    [Theory]
    [InlineData("""{"errors":{"Email":["", "  ", null, 42, ["x"], "The Email field is required."],"Age":42,"Name":null,"City":{}}}""")]
    [InlineData("""{"errors":{"Age":"  ","Email":["The Email field is required."]}}""")]
    public async Task ToResultAsync_ErrorsObjectWithValuesThatAreNotMessages_DropsThem(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ErrorDetails.ShouldBe([new ErrorDetail("/Email", "The Email field is required.")]);
    }

    #endregion

    #region detailTemplated

    [Fact]
    public async Task ToResultAsync_DetailTemplated_IsTheTemplatedMessageOfTheError()
    {
        // Arrange
        const string Body = """{"detailTemplated":{"templateId":"credit.insufficient","params":{"balance":30,"price":50.5,"currency":"EUR","final":true}}}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, Body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.ShouldBe(new TemplatedMessage("credit.insufficient", ("balance", 30), ("price", 50.5m), ("currency", "EUR"), ("final", true)));
        error.DetailTemplated.Parameters.ShouldNotBeNull();
        error.DetailTemplated.Parameters.Keys.ShouldContain("price");
        error.DetailTemplated.Parameters["price"].ShouldBeOfType<decimal>();
    }

    [Theory]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient"}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":null}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":{}}}""")]
    public async Task ToResultAsync_DetailTemplatedWithoutParameters_HasNoParameters(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe("credit.insufficient");
        error.DetailTemplated.Parameters.ShouldBeNull();
    }

    // A template that the core rejects is dropped as a whole. Regression (2.x read messageId).
    [Theory]
    [InlineData("""{"detailTemplated":{"params":{"balance":30}}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"","params":{"balance":30}}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"  "}}""")]
    [InlineData("""{"detailTemplated":{"templateId":" credit.insufficient"}}""")]
    [InlineData("""{"detailTemplated":{"templateId":42}}""")]
    [InlineData("""{"detailTemplated":{"templateId":null}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":{"min":1,"Min":2}}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":{"min":1,"min":2}}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":{" min":1}}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":[30]}}""")]
    [InlineData("""{"detailTemplated":{"templateId":"credit.insufficient","params":"balance"}}""")]
    [InlineData("""{"detailTemplated":{"messageId":"credit.insufficient","params":{"balance":30}}}""")]
    [InlineData("""{"detailTemplated":"credit.insufficient"}""")]
    [InlineData("""{"detailTemplated":null}""")]
    public async Task ToResultAsync_InvalidDetailTemplated_IsDropped(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.DetailTemplated.ShouldBeNull();
        error.Metadata.Count.ShouldBe(1);
    }

    // Template parameters are read as the values of the metadata.
    [Fact]
    public async Task ToResultAsync_DetailTemplatedWithNestedParameters_ReadsThemAsPlainValues()
    {
        // Arrange
        const string Body = """{"detailTemplated":{"templateId":"items.invalid","params":{"ids":[1,2],"range":{"min":1,"max":3000000000},"note":null}}}""";
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.BadRequest, Body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.DetailTemplated.ShouldNotBeNull();
        var parameters = error.DetailTemplated.Parameters;
        parameters.ShouldNotBeNull();
        parameters.Keys.ShouldContain("ids");
        var ids = parameters["ids"].ShouldBeOfType<object?[]>();
        ids.ShouldBe(new object?[] { 1, 2 });
        parameters.Keys.ShouldContain("range");
        var range = parameters["range"].ShouldBeOfType<Dictionary<string, object?>>();
        range.Keys.ShouldContain("min");
        range["min"].ShouldBe(1);
        range.Keys.ShouldContain("max");
        range["max"].ShouldBe(3000000000L);
        parameters.Keys.ShouldContain("note");
        parameters["note"].ShouldBeNull();
    }

    #endregion

    #region metadata

    // Every other member is an extension member (RFC 9457, section 3.2). Regression (2.x dropped them).
    [Fact]
    public async Task ToResultAsync_ExtensionMembers_AreTheMetadata()
    {
        // Arrange
        const string Body = """
            {
              "title": "You do not have enough credit.",
              "traceId": "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00",
              "balance": 30,
              "price": 50.5,
              "retryable": false,
              "reason": null
            }
            """;

        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, Body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Metadata.Count.ShouldBe(6);
        error.TryGetMetadata<string>("traceId", out var traceId).ShouldBeTrue();
        traceId.ShouldBe("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00");
        error.TryGetMetadata<int>("balance", out var balance).ShouldBeTrue();
        balance.ShouldBe(30);
        error.TryGetMetadata<decimal>("price", out var price).ShouldBeTrue();
        price.ShouldBe(50.5m);
        error.TryGetMetadata<bool>("retryable", out var retryable).ShouldBeTrue();
        retryable.ShouldBeFalse();
        error.Metadata.Keys.ShouldContain("reason");
        error.Metadata["reason"].ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_ExtensionMemberWithAnObject_IsADictionary()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"account":{"id":"A-1","limits":[10,20]}}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Metadata.Keys.ShouldContain("account");
        var account = error.Metadata["account"].ShouldBeOfType<Dictionary<string, object?>>();
        account.Keys.ShouldContain("id");
        account["id"].ShouldBe("A-1");
        account.Keys.ShouldContain("limits");
        var limits = account["limits"].ShouldBeOfType<object?[]>();
        limits.ShouldBe(new object?[] { 10, 20 });
    }

    // A key that the core rejects is dropped: a reserved member in another case, white space at the ends, or a blank key.
    [Theory]
    [InlineData("""{"Type":"x"}""")]
    [InlineData("""{"TITLE":"x"}""")]
    [InlineData("""{"Status":400}""")]
    [InlineData("""{"Detail":"x"}""")]
    [InlineData("""{"Instance":"x"}""")]
    [InlineData("""{"ERRORS":[]}""")]
    [InlineData("""{"DetailTemplated":{}}""")]
    [InlineData("""{" traceId":"x"}""")]
    [InlineData("""{"traceId ":"x"}""")]
    [InlineData("""{"":"x"}""")]
    [InlineData("""{"   ":"x"}""")]
    public async Task ToResultAsync_ExtensionMemberWithAKeyThatTheCoreRejects_IsDropped(string body)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, body);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Metadata.Count.ShouldBe(1);
        error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
    }

    // The keys of the metadata are compared ignoring case (so are the lookups of Keys): the last member is kept, with its spelling.
    [Fact]
    public async Task ToResultAsync_ExtensionMembersThatDifferOnlyInCase_KeepTheLastOne()
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"traceId":"first","TraceId":"second"}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Metadata.Keys.ShouldBe(["TraceId", HttpErrorMetadataKeys.HttpStatusCode], ignoreOrder: true);
        error.Metadata["TraceId"].ShouldBe("second");
    }

    // The entries about the response are set after the body is read, so they replace a member of the same name.
    [Theory]
    [InlineData("httpStatusCode")]
    [InlineData("HttpStatusCode")]
    public async Task ToResultAsync_ExtensionMemberWithTheKeyOfTheStatusCode_IsReplaced(string key)
    {
        // Arrange
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.NotFound, $$"""{"{{key}}":200}""");

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.Metadata.Count.ShouldBe(1);
        error.Metadata.Keys.ShouldContain(HttpErrorMetadataKeys.HttpStatusCode);
        error.Metadata[HttpErrorMetadataKeys.HttpStatusCode].ShouldBe(404);
        error.TryGetHttpStatusCode(out var statusCode).ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ConfigureError runs after the body was read.
    [Fact]
    public async Task ToResultAsync_ConfigureError_ReceivesTheProblemDetails()
    {
        // Arrange
        string? traceId = null;
        var options = new HttpResultOptions { ConfigureError = (_, builder) => traceId = builder.Build().Metadata["traceId"] as string };
        using var response = ResponseFactory.CreateProblemDetails(HttpStatusCode.Forbidden, """{"traceId":"abc"}""");

        // Act
        var error = await ReadErrorAsync(response, options);

        // Assert
        traceId.ShouldBe("abc");
        error.Metadata["traceId"].ShouldBe("abc");
    }

    #endregion

    #region round trip

    // An error written in the format of the core by another service reads back equal, apart from the entries about the response.
    [Fact]
    public async Task ToResultAsync_ErrorWrittenInTheFormatOfTheCore_ReadsBackEqual()
    {
        // Arrange
        var expected = ErrorBuilder
            .Validation(ErrorUri.Tag("tag:example.com,2026:signup.invalid"), "The sign-up data is not valid.")
            .WithDetail("Two fields are not valid.")
            .WithDetailTemplated("signup.invalid", ("count", 2))
            .WithInstanceUri(ErrorUri.Relative("/signup/requests/4815162342"))
            .AddErrorDetail(new ErrorDetail("/email", "The email address is required.", new TemplatedMessage("signup.email.required")))
            .AddErrorDetail(new ErrorDetail("/age", "The age must be at least 18.", new TemplatedMessage("signup.age.min", ("min", 18), ("ratio", 0.5m))))
            .AddErrorDetail(new ErrorDetail(null, "The start date is after the end date."))
            .WithMetadata("traceId", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00")
            .WithMetadata("retryable", false)
            .WithMetadata("attempt", 3)
            .WithMetadata("quota", 3000000000L)
            .WithMetadata("reason", null)
            .Build();

        var json = ProblemDetailsWriter.Write(expected);
        using var httpClient = CreateHttpClient(HttpStatusCode.BadRequest, json);
        using var response = await httpClient.GetAsync("https://api.example.com/signup", CancellationToken);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        var errorWithoutTheStatusCode = error.WithoutMetadata(HttpErrorMetadataKeys.HttpStatusCode);
        errorWithoutTheStatusCode.ShouldBe(expected);
    }

    // The example of docs/problem-details.md of the core.
    [Fact]
    public async Task ToResultAsync_ExampleOfTheCore_ReadsTheError()
    {
        // Arrange
        const string Body = """
            {
              "type": "https://exampleapp.com/docs/errors/signup/validation",
              "title": "The sign-up data is not valid.",
              "status": 400,
              "detail": "Two fields are not valid.",
              "instance": "/signup/requests/4815162342",
              "errors": [
                {
                  "pointer": "/email",
                  "detail": "The email address is required.",
                  "detailTemplated": { "templateId": "signup.email.required" }
                },
                {
                  "pointer": "/age",
                  "detail": "The age must be at least 18.",
                  "detailTemplated": { "templateId": "signup.age.min", "params": { "min": 18 } }
                }
              ],
              "retryable": false
            }
            """;

        var expected = ErrorBuilder
            .Validation(ErrorUri.Locator("https://exampleapp.com/docs/errors/signup/validation"), "The sign-up data is not valid.")
            .WithDetail("Two fields are not valid.")
            .WithInstanceUri(ErrorUri.Relative("/signup/requests/4815162342"))
            .AddErrorDetail(new ErrorDetail("/email", "The email address is required.", new TemplatedMessage("signup.email.required")))
            .AddErrorDetail(new ErrorDetail("/age", "The age must be at least 18.", new TemplatedMessage("signup.age.min", ("min", 18))))
            .WithMetadata("retryable", false)
            .WithMetadata(HttpErrorMetadataKeys.HttpStatusCode, 400)
            .Build();

        using var httpClient = CreateHttpClient(HttpStatusCode.BadRequest, Body);
        using var response = await httpClient.GetAsync("https://exampleapp.com/signup", CancellationToken);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ShouldBe(expected);
    }

    // The Body of ValidationProblem() of ASP.NET Core (ValidationProblemDetails).
    [Fact]
    public async Task ToResultAsync_ValidationProblemDetailsOfAspNetCore_ReadsTheError()
    {
        // Arrange
        const string Body = """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": {
                "Email": [ "The Email field is required." ],
                "Items[0].Quantity": [ "The field Quantity must be between 1 and 100." ]
              },
              "traceId": "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00"
            }
            """;

        var expected = ErrorBuilder
            .Validation(ErrorUri.Locator("https://tools.ietf.org/html/rfc9110#section-15.5.1"), "One or more validation errors occurred.")
            .AddErrorDetail(new ErrorDetail("/Email", "The Email field is required."))
            .AddErrorDetail(new ErrorDetail("/Items/0/Quantity", "The field Quantity must be between 1 and 100."))
            .WithMetadata("traceId", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00")
            .WithMetadata(HttpErrorMetadataKeys.HttpStatusCode, 400)
            .Build();

        using var httpClient = CreateHttpClient(HttpStatusCode.BadRequest, Body, "application/problem+json");
        using var response = await httpClient.GetAsync("https://api.example.com/orders", CancellationToken);

        // Act
        var error = await ReadErrorAsync(response);

        // Assert
        error.ShouldBe(expected);
    }

    #endregion

    #region helper methods

    private static System.Net.Http.HttpClient CreateHttpClient(HttpStatusCode statusCode, string body, string contentType = "application/problem+json; charset=utf-8")
    {
        var handler = new StaticResponseHandler(() => ResponseFactory.Create(statusCode, body, contentType));

        return new System.Net.Http.HttpClient(handler);
    }

    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response, HttpResultOptions? options = null)
    {
        var result = await response.ToResultAsync(options, CancellationToken);

        result.IsError.ShouldBeTrue();
        result.Error.ShouldNotBeNull();

        return result.Error;
    }

    #endregion
}
