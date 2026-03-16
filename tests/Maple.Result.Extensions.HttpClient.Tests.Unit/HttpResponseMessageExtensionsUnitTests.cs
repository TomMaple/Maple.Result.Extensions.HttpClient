using System.Net;
using System.Net.Http.Headers;
using Maple.Result.Extensions.HttpClient.Helpers;

namespace Maple.Result.Extensions.HttpClient.Tests.Unit;

public class HttpResponseMessageExtensionsUnitTests
{
    #region consts

    private const string BlankTypeUri = "about:blank";
    private const string FullErrorJson = """
                            {
                                "type": "https://example.com/probs/out-of-credit", 
                                "status": 400,
                                "title": "You do not have enough credit.",
                                "detail": "Your current balance is 30, but that costs 50.",
                                "instance": "https://example.com/accounts/12345/msgs/abc",
                                "errors": [
                                    {
                                        "pointer": "#/age",
                                        "detail": "must be a positive integer",
                                        "detailTemplated": {
                                            "messageId":"user.details.age.mustBePositive"
                                        }
                                    },
                                    {
                                        "pointer": "#/profile/colour",
                                        "detail": "must be ‘green’, ‘red’ or ‘blue’",
                                        "detailTemplated": {
                                            "messageId": "user.profile.colour",
                                            "params": {
                                                "validValueIds": [
                                                    "user.profile.colour.green",
                                                    "user.profile.colour.red",
                                                    "user.profile.colour.blue"
                                                ]
                                            }
                                        }
                                    }
                                ],
                                "detailTemplated": {
                                    "messageId": "user.account.balance.tooLow",
                                    "params": {
                                        "errorCode": "UAB17",
                                        "accounts": [
                                            {
                                                "title": "Main (***9456)",
                                                "url": "/accounts/12345"
                                            },
                                            {
                                                "title": "Main (***3357)",
                                                "url": "/accounts/67890"
                                            }
                                        ],
                                        "currentBalance": 30,
                                        "requiredBalance": 50
                                    }
                                }
                            
                            }
                            """;

    private const string CustomErrorResponseJson = """
                                                   {
                                                       "code": 1234,
                                                       "message": {
                                                           "en": "Custom error message",
                                                           "fr": "Message d'erreur personnalisé"
                                                       },
                                                       "errors": {
                                                           "value1": {
                                                               "en": "error1",
                                                               "fr": "erreur1"
                                                           }
                                                       }
                                                   }
                                                   """;

    private const string ExpectedTypeUri = "https://example.com/probs/out-of-credit";
    private const string ExpectedTitle = "You do not have enough credit.";
    private const string ExpectedDetail = "Your current balance is 30, but that costs 50.";
    private const string ExpectedInstanceUri = "https://example.com/accounts/12345/msgs/abc";
    private const string ExpectedDetailTemplatedMessageId = "user.account.balance.tooLow";

    private static readonly Dictionary<string, object> ExpectedAccountErrorDetail1 = new()
    {
        ["title"] = "Main (***9456)",
        ["url"] = "/accounts/12345"
    };

    private static readonly Dictionary<string, object> ExpectedAccountErrorDetail2 = new()
    {
        ["title"] = "Main (***3357)",
        ["url"] = "/accounts/67890"
    };

    private static readonly IReadOnlyList<ErrorDetail> ExpectedErrors =
    [
        new("#/age","must be a positive integer", new TemplatedMessage("user.details.age.mustBePositive")),
        new("#/profile/colour","must be ‘green’, ‘red’ or ‘blue’", new TemplatedMessage("user.profile.colour", new Dictionary<string, object>
        {
            ["validValueIds"] = new[]
            {
                "user.profile.colour.green",
                "user.profile.colour.red",
                "user.profile.colour.blue"
            }
        }))
    ];

    #endregion

    #region Result

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_ErrorResponseWithEmptyTextContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(string.Empty) };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_ErrorResponseWithEmptyTextContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(string.Empty) };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_ErrorResponseWithEmptyTextContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(string.Empty) };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_ErrorResponseWithTextContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent("Request failed") };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_ErrorResponseWithTextContent_ReturnsFailedResultWithTextContent(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Request failed";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent("Request failed") };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_ErrorResponseWithTextContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent("Request failed") };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_ErrorResultResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(FullErrorJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_ErrorResultResponse_ReturnsFailedResultWithFilledProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(FullErrorJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(ExpectedTypeUri);
        result.Error.Title.ShouldBe(ExpectedTitle);
        result.Error.Detail.ShouldBe(ExpectedDetail);
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(ExpectedDetailTemplatedMessageId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldContainKey("errorCode");
        result.Error.DetailTemplated.Params["errorCode"].ShouldBe("UAB17");
        result.Error.DetailTemplated.Params.ShouldContainKey("currentBalance");
        result.Error.DetailTemplated.Params["currentBalance"].ShouldBe(30);
        result.Error.DetailTemplated.Params.ShouldContainKey("requiredBalance");
        result.Error.DetailTemplated.Params["requiredBalance"].ShouldBe(50);
        result.Error.DetailTemplated.Params.ShouldContainKey("accounts");
        result.Error.DetailTemplated.Params["accounts"].ShouldBeOfType<object[]>();
        ((object[])result.Error.DetailTemplated.Params["accounts"]).Length.ShouldBe(2);

        var accountsObj1 = ((object[])result.Error.DetailTemplated.Params["accounts"])[0];
        accountsObj1.ShouldBeOfType<Dictionary<string, object>>();
        var accountsDict1 = (Dictionary<string, object>)accountsObj1;
        accountsDict1.ShouldBe(ExpectedAccountErrorDetail1);

        var accountsObj2 = ((object[])result.Error.DetailTemplated.Params["accounts"])[1];
        accountsObj2.ShouldBeOfType<Dictionary<string, object>>();
        var accountsDict2 = (Dictionary<string, object>)accountsObj2;
        accountsDict2.ShouldBe(ExpectedAccountErrorDetail2);

        result.Error.InstanceUri.ShouldBe(ExpectedInstanceUri);
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrors.Count);
        result.Error.ErrorDetails[0].ShouldBe(ExpectedErrors[0]);
        result.Error.ErrorDetails[1].PropertyPointer.ShouldBe(ExpectedErrors[1].PropertyPointer);
        result.Error.ErrorDetails[1].Detail.ShouldBe(ExpectedErrors[1].Detail);
        result.Error.ErrorDetails[1].DetailTemplated.ShouldNotBeNull();
        result.Error.ErrorDetails[1].DetailTemplated!.TemplateId.ShouldBe(ExpectedErrors[1].DetailTemplated!.TemplateId);
        result.Error.ErrorDetails[1].DetailTemplated!.Params.ShouldNotBeNull();

        result.Error.ErrorDetails[1].DetailTemplated!.Params.ShouldBeOfType<Dictionary<string, object>>();
        var detailsDict = (Dictionary<string, object>)result.Error.ErrorDetails[1].DetailTemplated!.Params!;
        detailsDict.ShouldContainKey("validValueIds");
        detailsDict["validValueIds"].ShouldBeOfType<object[]>();
        var validValueIds = (object[])detailsDict["validValueIds"];
        validValueIds.ShouldBe(ExpectedErrors[1].DetailTemplated!.Params!["validValueIds"]);
    }

    #endregion

    #region Result<TError> (Action<TError?, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TErrorErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TErrorErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ErrorResponseWithStringContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        const string ErrorContent = "Error response content";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(ErrorContent) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ErrorResponseWithStringContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ErrorContent = "Error response content";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(ErrorContent) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ErrorContent);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ErrorResponseWithStringContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        const string ErrorContent = "Error response content";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(ErrorContent) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson)};

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region Result<TError> (Action<TError?, HttpStatusCode, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error with HTTP status code: BadRequest")]
    [InlineData(HttpStatusCode.Unauthorized, "Error with HTTP status code: Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Error with HTTP status code: Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Error with HTTP status code: NotFound")]
    [InlineData(HttpStatusCode.RequestTimeout, "Error with HTTP status code: RequestTimeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Error with HTTP status code: GatewayTimeout")]
    [InlineData(HttpStatusCode.Conflict, "Error with HTTP status code: Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Error with HTTP status code: UnprocessableEntity")]
    [InlineData(HttpStatusCode.InternalServerError, "Error with HTTP status code: InternalServerError")]
    [InlineData(HttpStatusCode.NotImplemented, "Error with HTTP status code: NotImplemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Error with HTTP status code: ServiceUnavailable")]
    public async Task ToResultAsync_TErrorHttpStatusCodeErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode, string expectedErrorDetail)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBe(expectedErrorDetail);
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region Result<TError> (Action<TError?, HttpStatusCode, HttpResponseHeaders, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error with HTTP status code: BadRequest")]
    [InlineData(HttpStatusCode.Unauthorized, "Error with HTTP status code: Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Error with HTTP status code: Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Error with HTTP status code: NotFound")]
    [InlineData(HttpStatusCode.RequestTimeout, "Error with HTTP status code: RequestTimeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Error with HTTP status code: GatewayTimeout")]
    [InlineData(HttpStatusCode.Conflict, "Error with HTTP status code: Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Error with HTTP status code: UnprocessableEntity")]
    [InlineData(HttpStatusCode.InternalServerError, "Error with HTTP status code: InternalServerError")]
    [InlineData(HttpStatusCode.NotImplemented, "Error with HTTP status code: NotImplemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Error with HTTP status code: ServiceUnavailable")]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode, string expectedErrorDetail)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBe(expectedErrorDetail);
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region Result<TError> (Action<TError?, HttpStatusCode, HttpResponseHeaders, String, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error with HTTP status code: BadRequest (225)")]
    [InlineData(HttpStatusCode.Unauthorized, "Error with HTTP status code: Unauthorized (225)")]
    [InlineData(HttpStatusCode.Forbidden, "Error with HTTP status code: Forbidden (225)")]
    [InlineData(HttpStatusCode.NotFound, "Error with HTTP status code: NotFound (225)")]
    [InlineData(HttpStatusCode.RequestTimeout, "Error with HTTP status code: RequestTimeout (225)")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Error with HTTP status code: GatewayTimeout (225)")]
    [InlineData(HttpStatusCode.Conflict, "Error with HTTP status code: Conflict (225)")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Error with HTTP status code: UnprocessableEntity (225)")]
    [InlineData(HttpStatusCode.InternalServerError, "Error with HTTP status code: InternalServerError (225)")]
    [InlineData(HttpStatusCode.NotImplemented, "Error with HTTP status code: NotImplemented (225)")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Error with HTTP status code: ServiceUnavailable (225)")]
    public async Task ToResultAsync_TErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode, string expectedErrorDetail)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBe(expectedErrorDetail);
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region Result<T>

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithIntValue_ReturnsSuccessfulResultWithIntValue(HttpStatusCode statusCode)
    {
        // Arrange
        const int Value = 4782;
        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(Value.ToString())};

        // Act
        var result = await httpResponseMessage.ToResultAsync<int>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithDecimalValue_ReturnsSuccessfulResultWithDecimalValue(HttpStatusCode statusCode)
    {
        // Arrange
        const decimal Value = 879.263m;
        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(Value.ToString())};

        // Act
        var result = await httpResponseMessage.ToResultAsync<decimal>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithEnumValue_ReturnsSuccessfulResultWithEnumValue(HttpStatusCode statusCode)
    {
        // Arrange
        const HttpStatusCode Value = HttpStatusCode.NotFound;
        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(Value.ToString())};

        // Act
        var result = await httpResponseMessage.ToResultAsync<HttpStatusCode>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithQuotedEnumValue_ReturnsSuccessfulResultWithEnumValue(HttpStatusCode statusCode)
    {
        // Arrange
        const HttpStatusCode Value = HttpStatusCode.NotFound;
        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent($"\"{Value}\"")};

        // Act
        var result = await httpResponseMessage.ToResultAsync<HttpStatusCode>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithStringValue_ReturnsSuccessfulResultWithIntValue(HttpStatusCode statusCode)
    {
        // Arrange
        const string Value = "The user has been created.";
        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(Value)};

        // Act
        var result = await httpResponseMessage.ToResultAsync<string>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithQuotedStringValue_ReturnsSuccessfulResultWithIntValue(HttpStatusCode statusCode)
    {
        // Arrange
        const string Value = "The user has been created.";
        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent($"\"{Value}\"")};
        // Act
        var result = await httpResponseMessage.ToResultAsync<string>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(Value);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithStructValue_ReturnsSuccessfulResultWithStructValue(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"EmailAddress":"test@example.com","EmailName":"Test User"}""";
        var expectedValue = new EmailDetail("test@example.com", "Test User");

        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(TextValue)};

        // Act
        var result = await httpResponseMessage.ToResultAsync<EmailDetail>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
        result.Value.EmailAddress.ShouldBe(expectedValue.EmailAddress);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithRecordValue_ReturnsSuccessfulResultWithRecordValue(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"Id":965678,"DisplayName":"Test User","Name":"John Doe"}""";
        var expectedValue = new UserDetail(965678,"Test User", "John Doe");

        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(TextValue)};

        // Act
        var result = await httpResponseMessage.ToResultAsync<UserDetail>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithRecordStructValue_ReturnsSuccessfulResultWithRecordStructValue(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"CountryCode":"+1","Number":"(123) 456-7890"}""";
        var expectedValue = new PhoneNumber("+1","(123) 456-7890");

        var httpResponseMessage = new HttpResponseMessage(statusCode){Content = new StringContent(TextValue)};

        // Act
        var result = await httpResponseMessage.ToResultAsync<PhoneNumber>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_SuccessfulResponseWithClassValue_ReturnsSuccessfulResultWithClassValue(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"Id":"0cc844ae-8a9a-4c89-9034-22032ae19c39","DisplayName":"Test User","FirstName":"John","LastName":"Doe","YearOfBirth":1990}""";
        var expectedValue = new User
        {
            Id = new Guid("0cc844ae-8a9a-4c89-9034-22032ae19c39"),
            DisplayName = "Test User",
            FirstName = "John",
            LastName = "Doe",
            YearOfBirth = 1990
        };

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(expectedValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_ErrorResultWithoutResponse_ReturnsFailedValueResultWithDefaultTitle(
        HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Title.ShouldBe(expectedErrorTitle);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }


    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_ErrorResultResponse_ReturnsFailedValueResultWithFilledProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(FullErrorJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(ExpectedTypeUri);
        result.Error.Title.ShouldBe(ExpectedTitle);
        result.Error.Detail.ShouldBe(ExpectedDetail);
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(ExpectedDetailTemplatedMessageId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldContainKey("errorCode");
        result.Error.DetailTemplated.Params["errorCode"].ShouldBe("UAB17");
        result.Error.DetailTemplated.Params.ShouldContainKey("currentBalance");
        result.Error.DetailTemplated.Params["currentBalance"].ShouldBe(30);
        result.Error.DetailTemplated.Params.ShouldContainKey("requiredBalance");
        result.Error.DetailTemplated.Params["requiredBalance"].ShouldBe(50);
        result.Error.DetailTemplated.Params.ShouldContainKey("accounts");
        result.Error.DetailTemplated.Params["accounts"].ShouldBeOfType<object[]>();
        ((object[])result.Error.DetailTemplated.Params["accounts"]).Length.ShouldBe(2);

        var accountsObj1 = ((object[])result.Error.DetailTemplated.Params["accounts"])[0];
        accountsObj1.ShouldBeOfType<Dictionary<string, object>>();
        var accountsDict1 = (Dictionary<string, object>)accountsObj1;
        accountsDict1.ShouldBe(ExpectedAccountErrorDetail1);

        var accountsObj2 = ((object[])result.Error.DetailTemplated.Params["accounts"])[1];
        accountsObj2.ShouldBeOfType<Dictionary<string, object>>();
        var accountsDict2 = (Dictionary<string, object>)accountsObj2;
        accountsDict2.ShouldBe(ExpectedAccountErrorDetail2);

        result.Error.InstanceUri.ShouldBe(ExpectedInstanceUri);
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrors.Count);
        result.Error.ErrorDetails[0].ShouldBe(ExpectedErrors[0]);
        result.Error.ErrorDetails[1].PropertyPointer.ShouldBe(ExpectedErrors[1].PropertyPointer);
        result.Error.ErrorDetails[1].Detail.ShouldBe(ExpectedErrors[1].Detail);
        result.Error.ErrorDetails[1].DetailTemplated.ShouldNotBeNull();
        result.Error.ErrorDetails[1].DetailTemplated!.TemplateId.ShouldBe(ExpectedErrors[1].DetailTemplated!.TemplateId);
        result.Error.ErrorDetails[1].DetailTemplated!.Params.ShouldNotBeNull();

        result.Error.ErrorDetails[1].DetailTemplated!.Params.ShouldBeOfType<Dictionary<string, object>>();
        var detailsDict = (Dictionary<string, object>)result.Error.ErrorDetails[1].DetailTemplated!.Params!;
        detailsDict.ShouldContainKey("validValueIds");
        detailsDict["validValueIds"].ShouldBeOfType<object[]>();
        var validValueIds = (object[])detailsDict["validValueIds"];
        validValueIds.ShouldBe(ExpectedErrors[1].DetailTemplated!.Params!["validValueIds"]);
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidIntValue_ReturnsFailedResult()
    {
        // Arrange
        const string InputValue = "45m8";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(InputValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<int>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidDecimalValue_ReturnsFailedResult()
    {
        // Arrange
        const string InputValue = "Zero";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(InputValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<int>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidEnumValue_ReturnsFailedResult()
    {
        // Arrange
        const string InputValue = "Unauthenticated";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(InputValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<HttpStatusCode>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidQuotedEnumValue_ReturnsFailedResult()
    {
        // Arrange
        const string InputValue = "Unauthenticated";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"\"{InputValue}\"") };

        // Act
        var result = await httpResponseMessage.ToResultAsync<HttpStatusCode>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidStructValueTypes_ReturnsFailedResult()
    {
        // Arrange
        const string TextValue = """{"EmailAddress":9,"EmailName":true}""";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<EmailDetail>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Theory]
    [InlineData("""{"AnotherProperty":"test@example.com","NotExistingProperty":"Test User"}""")]
    [InlineData("{}")]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidStructValue_ReturnsEmptyResult(string textValue)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(textValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<EmailDetail>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.EmailAddress.ShouldBeNull();
        result.Value.EmailName.ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidRecordValueTypes_ReturnsFailedResult()
    {
        // Arrange
        const string TextValue = """{"Id":"invalidId","EmailAddress":9,"EmailName":true}""";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<UserDetail>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Theory]
    [InlineData("""{"AnotherProperty":"test@example.com","NotExistingProperty":"Test User"}""")]
    [InlineData("{}")]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidRecordValue_ReturnsEmptyResult(string textValue)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(textValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<UserDetail>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(0);
        result.Value.DisplayName.ShouldBeNull();
        result.Value.Name.ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidRecordStructValue_ReturnsFailedResult()
    {
        // Arrange
        const string TextValue = """{"CountryCode":1,"EmailAddress":9,"EmailName":true}""";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<PhoneNumber>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Theory]
    [InlineData("""{"AnotherProperty":"test@example.com","NotExistingProperty":"Test User"}""")]
    [InlineData("{}")]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidRecordStructValue_ReturnsEmptyResult(string textValue)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(textValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<PhoneNumber>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.CountryCode.ShouldBeNull();
        result.Value.Number.ShouldBeNull();
    }

    [Theory]
    [InlineData("""{"AnotherProperty":"test@example.com","NotExistingProperty":"Test User"}""")]
    [InlineData("{}")]
    public async Task ToResultAsync_SuccessfulResponseWithInvalidClassValue_ReturnsEmptyResult(string textValue)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(textValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(Guid.Empty);
        result.Value.DisplayName.ShouldBeEmpty();
        result.Value.FirstName.ShouldBeEmpty();
        result.Value.LastName.ShouldBeEmpty();
        result.Value.YearOfBirth.ShouldBe((ushort)0);
    }

    #endregion

    #region Result<T, TError> (Action<TError?, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TTErrorErrorBuilder_SuccessfulResponseWithValueAndErrorType_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"Id":"0cc844ae-8a9a-4c89-9034-22032ae19c39","DisplayName":"Test User","FirstName":"John","LastName":"Doe","YearOfBirth":1990}""";
        var expectedValue = new User
        {
            Id = new Guid("0cc844ae-8a9a-4c89-9034-22032ae19c39"),
            DisplayName = "Test User",
            FirstName = "John",
            LastName = "Doe",
            YearOfBirth = 1990
        };

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(expectedValue);
    }

    [Fact]
    public async Task ToResultAsync_TTErrorErrorBuilder_SuccessfulResponseWithInvalidValueAndErrorType_ReturnsFailedResult()
    {
        // Arrange
        const string TextValue = """{"Id":111,"InvalidProperty":222,"YearOfBirth":1990}""";
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe("tag:mapledev.engineer,2026:result.httpClient.successResponse.json.invalid");
        result.Error.Title.ShouldBe("Failed to deserialize the HTTP response content.");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorErrorBuilder_CustomErrorResponseAndErrorContent_ReturnsFailedValueResultWithMappedProperties(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        const string ExpectedErrorTitle = "The report has been rejected";
        const int ExpectedErrorDetailsCount = 2;
        const string ExpectedErrorDetail1Pointer = "#/username";
        const string ExpectedErrorDetail1Message = "At least 3 characters required";
        const string ExpectedErrorDetail2Pointer = "#/email";
        const string ExpectedErrorDetail2Message = "Required.";

        var json = $$"""
                     {
                        "Code": {{(int)statusCode}},
                        "Message": {
                            "en": "The report has been rejected",
                            "fr": "Le rapport a été rejeté"
                        },
                        "Errors": {
                            "#/username": {
                                "en": "At least 3 characters required",
                                "fr": "Au moins 3 caractères requis"
                            },
                            "#/email": {
                                "en": "Required.",
                                "fr": "Requis."
                            }
                        }
                    }
                    """;

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(json) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetail1Pointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetail1Message);
        result.Error.ErrorDetails[0].DetailTemplated.ShouldBeNull();
        result.Error.ErrorDetails[1].PropertyPointer.ShouldBe(ExpectedErrorDetail2Pointer);
        result.Error.ErrorDetails[1].Detail.ShouldBe(ExpectedErrorDetail2Message);
        result.Error.ErrorDetails[1].DetailTemplated.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable, "Service Unavailable")]
    public async Task ToResultAsync_TTErrorErrorBuilder_CustomErrorResponseWithoutContent_ReturnsFailedValueResultWithDefaultTitle(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map1);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Title.ShouldBe(expectedErrorTitle);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    #endregion

    #region Result<T, TError> (Action<TError?, HttpStatusCode, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"Id":"0cc844ae-8a9a-4c89-9034-22032ae19c39","DisplayName":"Test User","FirstName":"John","LastName":"Doe","YearOfBirth":1990}""";
        var expectedValue = new User
        {
            Id = new Guid("0cc844ae-8a9a-4c89-9034-22032ae19c39"),
            DisplayName = "Test User",
            FirstName = "John",
            LastName = "Doe",
            YearOfBirth = 1990
        };

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(expectedValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error with HTTP status code: BadRequest")]
    [InlineData(HttpStatusCode.Unauthorized, "Error with HTTP status code: Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Error with HTTP status code: Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Error with HTTP status code: NotFound")]
    [InlineData(HttpStatusCode.RequestTimeout, "Error with HTTP status code: RequestTimeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Error with HTTP status code: GatewayTimeout")]
    [InlineData(HttpStatusCode.Conflict, "Error with HTTP status code: Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Error with HTTP status code: UnprocessableEntity")]
    [InlineData(HttpStatusCode.InternalServerError, "Error with HTTP status code: InternalServerError")]
    [InlineData(HttpStatusCode.NotImplemented, "Error with HTTP status code: NotImplemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Error with HTTP status code: ServiceUnavailable")]
    public async Task ToResultAsync_TTErrorHttpStatusCodeErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode, string expectedErrorDetail)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map2);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBe(expectedErrorDetail);
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region Result<TError> (Action<TError?, HttpStatusCode, HttpResponseHeaders, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"Id":"0cc844ae-8a9a-4c89-9034-22032ae19c39","DisplayName":"Test User","FirstName":"John","LastName":"Doe","YearOfBirth":1990}""";
        var expectedValue = new User
        {
            Id = new Guid("0cc844ae-8a9a-4c89-9034-22032ae19c39"),
            DisplayName = "Test User",
            FirstName = "John",
            LastName = "Doe",
            YearOfBirth = 1990
        };

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(expectedValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error with HTTP status code: BadRequest")]
    [InlineData(HttpStatusCode.Unauthorized, "Error with HTTP status code: Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Error with HTTP status code: Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Error with HTTP status code: NotFound")]
    [InlineData(HttpStatusCode.RequestTimeout, "Error with HTTP status code: RequestTimeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Error with HTTP status code: GatewayTimeout")]
    [InlineData(HttpStatusCode.Conflict, "Error with HTTP status code: Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Error with HTTP status code: UnprocessableEntity")]
    [InlineData(HttpStatusCode.InternalServerError, "Error with HTTP status code: InternalServerError")]
    [InlineData(HttpStatusCode.NotImplemented, "Error with HTTP status code: NotImplemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Error with HTTP status code: ServiceUnavailable")]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode, string expectedErrorDetail)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map3);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBe(expectedErrorDetail);
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region Result<TError> (Action<TError?, HttpStatusCode, HttpResponseHeaders, String, ErrorBuilder>)

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.PartialContent)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_SuccessfulResponseWithoutContent_ReturnsSuccessfulResult(HttpStatusCode statusCode)
    {
        // Arrange
        const string TextValue = """{"Id":"0cc844ae-8a9a-4c89-9034-22032ae19c39","DisplayName":"Test User","FirstName":"John","LastName":"Doe","YearOfBirth":1990}""";
        var expectedValue = new User
        {
            Id = new Guid("0cc844ae-8a9a-4c89-9034-22032ae19c39"),
            DisplayName = "Test User",
            FirstName = "John",
            LastName = "Doe",
            YearOfBirth = 1990
        };

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(TextValue) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(expectedValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
        result.Error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "Not Found")]
    [InlineData(HttpStatusCode.RequestTimeout, "Request Timeout")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Gateway Timeout")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Unprocessable Entity")]
    [InlineData(HttpStatusCode.InternalServerError, "Internal Server Error")]
    [InlineData(HttpStatusCode.NotImplemented, "Not Implemented")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithDefaultTitle(HttpStatusCode statusCode, string expectedErrorTitle)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(expectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ErrorResponseWithoutContent_ReturnsFailedResultWithEmptyOtherProperties(HttpStatusCode statusCode)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode);

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorCategory.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCategory.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCategory.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, ErrorCategory.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorCategory.Timeout)]
    [InlineData(HttpStatusCode.Conflict, ErrorCategory.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCategory.Failure)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCategory.Critical)]
    [InlineData(HttpStatusCode.NotImplemented, ErrorCategory.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCategory.Unavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ValidErrorResponse_ReturnsFailedResult(HttpStatusCode statusCode, ErrorCategory expectedErrorCategory)
    {
        // Arrange
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(expectedErrorCategory);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotImplemented)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedTitle(HttpStatusCode statusCode)
    {
        // Arrange
        const string ExpectedErrorTitle = "Custom error message";
        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe(ExpectedErrorTitle);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Error with HTTP status code: BadRequest (225)")]
    [InlineData(HttpStatusCode.Unauthorized, "Error with HTTP status code: Unauthorized (225)")]
    [InlineData(HttpStatusCode.Forbidden, "Error with HTTP status code: Forbidden (225)")]
    [InlineData(HttpStatusCode.NotFound, "Error with HTTP status code: NotFound (225)")]
    [InlineData(HttpStatusCode.RequestTimeout, "Error with HTTP status code: RequestTimeout (225)")]
    [InlineData(HttpStatusCode.GatewayTimeout, "Error with HTTP status code: GatewayTimeout (225)")]
    [InlineData(HttpStatusCode.Conflict, "Error with HTTP status code: Conflict (225)")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "Error with HTTP status code: UnprocessableEntity (225)")]
    [InlineData(HttpStatusCode.InternalServerError, "Error with HTTP status code: InternalServerError (225)")]
    [InlineData(HttpStatusCode.NotImplemented, "Error with HTTP status code: NotImplemented (225)")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Error with HTTP status code: ServiceUnavailable (225)")]
    public async Task ToResultAsync_TTErrorHttpStatusCodeHttpResponseHeadersStringErrorBuilder_ValidErrorResponse_ReturnsFailedResultWithMappedProperties(HttpStatusCode statusCode, string expectedErrorDetail)
    {
        // Arrange
        const int ExpectedErrorDetailsCount = 1;
        const string ExpectedErrorDetailPointer = "value1";
        const string ExpectedErrorDetailMessage = "error1";

        var httpResponseMessage = new HttpResponseMessage(statusCode) { Content = new StringContent(CustomErrorResponseJson) };

        // Act
        var result = await httpResponseMessage.ToResultAsync<User, CustomErrorResponse>(CustomErrorResponseMapper.Map4);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.TypeUri.ShouldBe(BlankTypeUri);
        result.Error.Detail.ShouldBe(expectedErrorDetail);
        result.Error.DetailTemplated.ShouldBeNull();
        result.Error.InstanceUri.ShouldBeNull();
        result.Error.ErrorDetails.Count.ShouldBe(ExpectedErrorDetailsCount);
        result.Error.ErrorDetails[0].PropertyPointer.ShouldBe(ExpectedErrorDetailPointer);
        result.Error.ErrorDetails[0].Detail.ShouldBe(ExpectedErrorDetailMessage);
    }

    #endregion

    #region helper types

    public struct EmailDetail(string EmailAddress, string? EmailName)
    {
        public string EmailAddress { get; init; } = EmailAddress;
        public string? EmailName { get; init; } = EmailName;
    }

    private record UserDetail(int Id, string DisplayName, string Name);

    private record struct PhoneNumber(string CountryCode, string Number);

    private class User
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public ushort YearOfBirth { get; init; }
    }

    private class CustomErrorResponse
    {
        public int Code { get; init; }
        public LocalizedMessage Message { get; init; } = new();
        public Dictionary<string, Dictionary<string, string>> Errors { get; init; } = new();
    }

    private class LocalizedMessage
    {
        public string En { get; init; } = string.Empty;
        public string Fr { get; init; } = string.Empty;
    }

    private static class CustomErrorResponseMapper
    {
        private const string EnglishCode = "en";

        internal static void Map1(CustomErrorResponse? customError, ErrorBuilder errorBuilder)
        {
            if (customError is null)
                return;

            errorBuilder.WithTitle(customError.Message.En);

            foreach (var error in customError.Errors)
            {
                var message = error.Value.GetValueOrDefault(EnglishCode);
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                errorBuilder.WithErrorDetail(error.Key, message);
            }
        }

        internal static void Map2(CustomErrorResponse? customError, HttpStatusCode statusCode, ErrorBuilder errorBuilder)
        {
            if (customError is null)
                return;

            var errorDescription = $"Error with HTTP status code: {statusCode}";
            errorBuilder
                .WithTitle(customError.Message.En)
                .WithDetail(errorDescription);

            foreach (var error in customError.Errors)
            {
                var message = error.Value.GetValueOrDefault(EnglishCode);
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                errorBuilder.WithErrorDetail(error.Key, message);
            }
        }

        internal static void Map3(CustomErrorResponse? customError, HttpStatusCode statusCode,
            HttpResponseHeaders headers, ErrorBuilder errorBuilder)
        {
            if (customError is null)
                return;

            var errorDescription = $"Error with HTTP status code: {statusCode}";
            errorBuilder
                .WithTitle(customError.Message.En)
                .WithDetail(errorDescription);

            foreach (var error in customError.Errors)
            {
                var message = error.Value.GetValueOrDefault(EnglishCode);
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                errorBuilder.WithErrorDetail(error.Key, message);
            }
        }

        internal static void Map4(CustomErrorResponse? customError, HttpStatusCode statusCode,
            HttpResponseHeaders headers, string errorContent, ErrorBuilder errorBuilder)
        {
            if (customError is null)
                return;

            var length = errorContent.ReplaceLineEndings("").Length;
            var errorDescription = $"Error with HTTP status code: {statusCode} ({length})";
            errorBuilder
                .WithTitle(customError.Message.En)
                .WithDetail(errorDescription);

            foreach (var error in customError.Errors)
            {
                var message = error.Value.GetValueOrDefault(EnglishCode);
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                errorBuilder.WithErrorDetail(error.Key, message);
            }
        }
    }

    #endregion
}
