using System.Net;
using System.Net.Http.Headers;

namespace Maple.Result.Extensions.HttpClient.Tests.Unit;

public class HttpResponseMessageExtensionsRetryAfterUnitTests
{
    #region consts

    private const string RetryAfterTemplateId = "maple.result.httpClient.errorResponse.retryAfter";
    private const string RawParamName = "retryAfter";
    private const string SecondsParamName = "retryAfterSeconds";
    private const string DateParamName = "retryAfterDate";

    private const string ProblemDetailsJson = """
                                              {
                                                  "title": "Slow down.",
                                                  "detailTemplated": {
                                                      "messageId": "api.rateLimit.exceeded",
                                                      "params": {
                                                          "limit": 100
                                                      }
                                                  }
                                              }
                                              """;

    #endregion

    [Fact]
    public async Task ToResultAsync_TooManyRequestsWithZeroRetryAfter_AddsZeroSeconds()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(0);
        result.Error.DetailTemplated.Params.Keys.ShouldContain(RawParamName);
        result.Error.DetailTemplated.Params[RawParamName].ShouldBe("0");
    }

    [Fact]
    public async Task ToResultAsync_TooManyRequestsWithInvalidRetryAfter_DoesNotAddTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("Retry-After", "soon");

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.DetailTemplated.ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_CustomMapperSetsTemplateWithoutParams_KeepsTemplateAndAddsRetryAfter()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated("custom.template"));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe("custom.template");
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(30);
    }

    [Fact]
    public async Task ToResultAsync_CustomMapperSetsSameParam_KeepsCustomMapperValue()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        var namedValues = new Dictionary<string, object> { [SecondsParamName] = 999 };

        // Act
        var result = await response.ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated("custom.template", namedValues));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(999);
    }

    [Fact]
    public async Task ToResultAsync_CustomMapperOverridesCategory_KeepsCategoryAndAddsRetryAfter()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync<object>((_, builder) =>
            builder.WithCategory(ErrorCategory.Failure));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Failure);
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(RetryAfterTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(30);
    }

    [Fact]
    public async Task ToResultAsync_FullCustomMapperOverload_AddsRetryAfter()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync<object>((_, _, _, _, _) => { });

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(30);
    }

    [Fact]
    public async Task ToResultAsyncOfTTError_CustomMapper_AddsRetryAfter()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync<string, object>((_, _) => { });

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(30);
    }

    [Fact]
    public async Task ToResultAsync_TooManyRequestsWithoutRetryAfter_ReturnsUnavailableErrorWithoutTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.Title.ShouldBe("Too Many Requests");
        result.Error.DetailTemplated.ShouldBeNull();
    }

    [Fact]
    public async Task ToResultAsync_TooManyRequestsWithRetryAfterSeconds_AddsSecondsToTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(120));

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(RetryAfterTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(120);
        result.Error.DetailTemplated.Params.Keys.ShouldContain(RawParamName);
        result.Error.DetailTemplated.Params[RawParamName].ShouldBe("120");
        result.Error.DetailTemplated.Params.Keys.ShouldNotContain(DateParamName);
    }

    [Fact]
    public async Task ToResultAsync_TooManyRequestsWithRetryAfterDate_AddsUtcDateToTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter =
            new RetryConditionHeaderValue(new DateTimeOffset(2026, 11, 10, 12, 30, 0, TimeSpan.Zero));

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(RetryAfterTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(DateParamName);
        result.Error.DetailTemplated.Params[DateParamName].ShouldBe("2026-11-10T12:30:00.0000000+00:00");
        result.Error.DetailTemplated.Params.Keys.ShouldContain(RawParamName);
        result.Error.DetailTemplated.Params[RawParamName].ShouldBe("Tue, 10 Nov 2026 12:30:00 GMT");
        result.Error.DetailTemplated.Params.Keys.ShouldNotContain(SecondsParamName);
    }

    [Fact]
    public async Task ToResultAsync_TooManyRequestsWithProblemDetailsTemplate_KeepsTemplateAndAddsRetryAfter()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(ProblemDetailsJson)
        };
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Title.ShouldBe("Slow down.");
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe("api.rateLimit.exceeded");
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain("limit");
        result.Error.DetailTemplated.Params["limit"].ShouldBe(100);
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(30);
    }

    [Fact]
    public async Task ToResultAsyncOfT_TooManyRequestsWithRetryAfterSeconds_AddsSecondsToTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));

        // Act
        var result = await response.ToResultAsync<string>();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(5);
    }

    [Fact]
    public async Task ToResultAsync_ServiceUnavailableWithRetryAfter_DoesNotAddTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(120));

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldBeNull();
    }
}
