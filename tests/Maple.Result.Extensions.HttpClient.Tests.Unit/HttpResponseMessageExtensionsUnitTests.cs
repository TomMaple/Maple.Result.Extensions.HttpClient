using System.Net;

namespace Maple.Result.Extensions.HttpClient.Tests.Unit;

public class HttpResponseMessageExtensionsUnitTests
{
    [Fact]
    public async Task ToResultAsync_ErrorResultResponse_ReturnsFailedResult()
    {
        // Arrange
        const string Json = """
                            {
                                "type": "https://example.com/probs/out-of-credit", 
                                "status": 400,
                                "title": "You do not have enough credit.",
                                "detail": "Your current balance is 30, but that costs 50.",
                                "instance": "/accounts/12345/msgs/abc",
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

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(Json)
        };

        // Act
        var result = await httpResponseMessage.ToResultAsync<int>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
    }
}