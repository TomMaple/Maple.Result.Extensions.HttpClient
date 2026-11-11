using System.Net;
using System.Net.Http.Headers;

namespace Maple.Result.Extensions.HttpClient.Tests.Unit.Helpers;

public class ErrorBuilderUnitTests
{
    #region consts

    private const string TemplateId = "api.rateLimit.exceeded";
    private const string OtherTemplateId = "api.rateLimit.other";
    private const string SecondsParamName = "retryAfterSeconds";

    #endregion

    [Fact]
    public async Task WithDetailTemplated_NamedValuesReusedAcrossResponses_DoesNotModifyCallerDictionary()
    {
        // Arrange
        var sharedParams = new Dictionary<string, object> { ["limit"] = 100 };

        // Act
        var first = await CreateResponse(120).ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(TemplateId, sharedParams));
        var second = await CreateResponse(5).ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(TemplateId, sharedParams));
        var third = await CreateResponse().ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(TemplateId, sharedParams));

        // Assert
        sharedParams.Count.ShouldBe(1);
        sharedParams.Keys.ShouldNotContain(SecondsParamName);

        first.ShouldNotBeNull();
        first.IsSuccess().ShouldBeFalse();
        first.Error.ShouldNotBeNull();
        first.Error.DetailTemplated.ShouldNotBeNull();
        first.Error.DetailTemplated.Params.ShouldNotBeNull();
        first.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        first.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(120);

        second.ShouldNotBeNull();
        second.IsSuccess().ShouldBeFalse();
        second.Error.ShouldNotBeNull();
        second.Error.DetailTemplated.ShouldNotBeNull();
        second.Error.DetailTemplated.Params.ShouldNotBeNull();
        second.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        second.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(5);

        third.ShouldNotBeNull();
        third.IsSuccess().ShouldBeFalse();
        third.Error.ShouldNotBeNull();
        third.Error.DetailTemplated.ShouldNotBeNull();
        third.Error.DetailTemplated.Params.ShouldNotBeNull();
        third.Error.DetailTemplated.Params.Keys.ShouldNotContain(SecondsParamName);
    }

    [Fact]
    public async Task WithDetailTemplated_TemplatedMessageReusedAcrossResponses_DoesNotModifyCallerDictionary()
    {
        // Arrange
        var sharedParams = new Dictionary<string, object> { ["limit"] = 100 };
        var sharedMessage = new TemplatedMessage(TemplateId, sharedParams);

        // Act
        var first = await CreateResponse(120).ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(sharedMessage));
        var second = await CreateResponse(5).ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(sharedMessage));

        // Assert
        sharedParams.Count.ShouldBe(1);
        sharedParams.Keys.ShouldNotContain(SecondsParamName);

        first.ShouldNotBeNull();
        first.IsSuccess().ShouldBeFalse();
        first.Error.ShouldNotBeNull();
        first.Error.DetailTemplated.ShouldNotBeNull();
        first.Error.DetailTemplated.Params.ShouldNotBeNull();
        first.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        first.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(120);

        second.ShouldNotBeNull();
        second.IsSuccess().ShouldBeFalse();
        second.Error.ShouldNotBeNull();
        second.Error.DetailTemplated.ShouldNotBeNull();
        second.Error.DetailTemplated.Params.ShouldNotBeNull();
        second.Error.DetailTemplated.Params.Keys.ShouldContain(SecondsParamName);
        second.Error.DetailTemplated.Params[SecondsParamName].ShouldBe(5);
    }

    [Fact]
    public async Task WithDetailTemplated_TemplatedMessageThenNamedValues_DoesNotModifyTemplatedMessageDictionary()
    {
        // Arrange
        var messageParams = new Dictionary<string, object> { ["limit"] = 100 };
        var message = new TemplatedMessage(TemplateId, messageParams);
        var namedValues = new Dictionary<string, object> { ["window"] = "1m" };

        // Act
        var result = await CreateResponse().ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(message).WithDetailTemplated(OtherTemplateId, namedValues));

        // Assert
        messageParams.Count.ShouldBe(1);
        messageParams.Keys.ShouldNotContain("window");

        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain("limit");
        result.Error.DetailTemplated.Params["limit"].ShouldBe(100);
        result.Error.DetailTemplated.Params.Keys.ShouldContain("window");
        result.Error.DetailTemplated.Params["window"].ShouldBe("1m");
    }

    [Fact]
    public async Task WithDetailTemplated_NamedValuesCalledTwice_MergesAndLastValueWins()
    {
        // Arrange
        var firstValues = new Dictionary<string, object> { ["limit"] = 100, ["window"] = "1m" };
        var secondValues = new Dictionary<string, object> { ["limit"] = 200 };

        // Act
        var result = await CreateResponse().ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(TemplateId, firstValues).WithDetailTemplated(TemplateId, secondValues));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Count.ShouldBe(2);
        result.Error.DetailTemplated.Params.Keys.ShouldContain("limit");
        result.Error.DetailTemplated.Params["limit"].ShouldBe(200);
        result.Error.DetailTemplated.Params.Keys.ShouldContain("window");
        result.Error.DetailTemplated.Params["window"].ShouldBe("1m");
    }

    [Fact]
    public async Task WithDetailTemplated_NullNamedValues_KeepsExistingParams()
    {
        // Arrange
        var namedValues = new Dictionary<string, object> { ["limit"] = 100 };

        // Act
        var result = await CreateResponse().ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(TemplateId, namedValues).WithDetailTemplated(OtherTemplateId));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain("limit");
        result.Error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public async Task WithDetailTemplated_NamedValuesAsKeyValuePairArray_AddsParams()
    {
        // Arrange
        KeyValuePair<string, object>[] namedValues = [new("limit", 100), new("window", "1m")];

        // Act
        var result = await CreateResponse().ToResultAsync<object>((_, builder) =>
            builder.WithDetailTemplated(TemplateId, namedValues));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldContain("limit");
        result.Error.DetailTemplated.Params["limit"].ShouldBe(100);
        result.Error.DetailTemplated.Params.Keys.ShouldContain("window");
        result.Error.DetailTemplated.Params["window"].ShouldBe("1m");
    }

    private static HttpResponseMessage CreateResponse(int? retryAfterSeconds = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        if (retryAfterSeconds is not null)
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds.Value));

        return response;
    }
}
