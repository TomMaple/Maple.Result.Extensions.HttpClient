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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_RetryableStatusWithZeroRetryAfter_AddsZeroSeconds(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_RetryableStatusWithInvalidRetryAfter_DoesNotAddTemplatedDetail(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_CustomMapperSetsTemplateWithoutParams_KeepsTemplateAndAddsRetryAfter(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync<object>((_, builder) => builder.WithDetailTemplated("custom.template"));

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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_CustomMapperSetsSameParam_KeepsCustomMapperValue(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        var namedValues = new Dictionary<string, object> { [SecondsParamName] = 999 };

        // Act
        var result = await response.ToResultAsync<object>((_, builder) => builder.WithDetailTemplated("custom.template", namedValues));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(999);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_CustomMapperOverridesCategory_KeepsCategoryAndAddsRetryAfter(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));

        // Act
        var result = await response.ToResultAsync<object>((_, builder) => builder.WithCategory(ErrorCategory.Failure));

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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_FullCustomMapperOverload_AddsRetryAfter(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsyncOfTTError_CustomMapper_AddsRetryAfter(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "Too Many Requests")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "Service Unavailable")]
    public async Task ToResultAsync_RetryableStatusWithoutRetryAfter_ReturnsUnavailableErrorWithoutTemplatedDetail(HttpStatusCode statusCode, string expectedTitle)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);

        // Act
        var result = await response.ToResultAsync();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.Title.ShouldBe(expectedTitle);
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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_RetryableStatusWithRetryAfterDate_AddsUtcDateToTemplatedDetail(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(new DateTimeOffset(2026, 11, 10, 12, 30, 0, TimeSpan.Zero));

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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsync_RetryableStatusWithProblemDetailsTemplate_KeepsTemplateAndAddsRetryAfter(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode) { Content = new StringContent(ProblemDetailsJson) };
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

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ToResultAsyncOfT_RetryableStatusWithRetryAfterSeconds_AddsSecondsToTemplatedDetail(HttpStatusCode statusCode)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);
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
    public async Task ToResultAsync_ServiceUnavailableWithRetryAfter_AddsSecondsToTemplatedDetail()
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
        result.Error.Category.ShouldBe(ErrorCategory.Unavailable);
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(RetryAfterTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        result.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(120);
        result.Error.DetailTemplated.Params.Keys.ShouldContain(RawParamName);
        result.Error.DetailTemplated.Params[RawParamName].ShouldBe("120");
    }

    [Fact]
    public async Task ToResultAsync_InternalServerErrorWithRetryAfter_DoesNotAddTemplatedDetail()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
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
