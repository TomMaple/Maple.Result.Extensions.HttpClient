using System.Net;
using System.Net.Http.Headers;
using Maple.Result.Extensions.HttpClient.Helpers;

namespace Maple.Result.Extensions.HttpClient.Tests.Unit.Helpers;

public class ErrorBuilderUnitTests
{
    #region consts

    private const string BlankTypeUri = "about:blank";
    private const string LocatorUri = "https://example.com/probs/some-error";
    private const string OtherLocatorUri = "https://example.com/probs/other-error";
    private const string TagUri = "tag:example.com,2026:some.error";
    private const string Title = "Some title";
    private const string OtherTitle = "Other title";
    private const string Detail = "Some detail";
    private const string TemplateId = "api.rateLimit.exceeded";
    private const string OtherTemplateId = "api.rateLimit.other";
    private const string DefaultTemplateId = "default.template";
    private const string SecondsParamName = "retryAfterSeconds";
    private const string Pointer = "#/age";
    private const string OtherPointer = "#/name";

    #endregion

    #region constructor

    [Fact]
    public void Ctor_TitleWithSurroundingWhitespace_TrimsTitle()
    {
        // Act
        var sut = new ErrorBuilder(ErrorCategory.Failure, $"  {Title}  ");

        // Assert
        var error = sut.Build();
        error.ShouldNotBeNull();
        error.Title.ShouldBe(Title);
    }

    [Fact]
    public void Ctor_NothingElseSet_BuildsErrorWithDefaults()
    {
        // Act
        var sut = new ErrorBuilder(ErrorCategory.Conflict, Title);

        // Assert
        var error = sut.Build();
        error.ShouldNotBeNull();
        error.Category.ShouldBe(ErrorCategory.Conflict);
        error.TypeUri.ShouldBe(BlankTypeUri);
        error.Title.ShouldBe(Title);
        error.Detail.ShouldBeNull();
        error.DetailTemplated.ShouldBeNull();
        error.InstanceUri.ShouldBeNull();
        error.ErrorDetails.ShouldBeEmpty();
    }

    #endregion

    #region fluent API

    [Fact]
    public void WithMethods_AnyArguments_ReturnSameBuilder()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act & Assert
        sut.WithCategory(ErrorCategory.Timeout).ShouldBeSameAs(sut);
        sut.WithTypeUri(ErrorUri.Locator(LocatorUri)).ShouldBeSameAs(sut);
        sut.WithTypeUri(null).ShouldBeSameAs(sut);
        sut.WithTitle(Title).ShouldBeSameAs(sut);
        sut.WithTitle(null).ShouldBeSameAs(sut);
        sut.WithDetail(Detail).ShouldBeSameAs(sut);
        sut.WithDetail(null).ShouldBeSameAs(sut);
        sut.WithDetailTemplated(new TemplatedMessage(TemplateId)).ShouldBeSameAs(sut);
        sut.WithDetailTemplated((TemplatedMessage?)null).ShouldBeSameAs(sut);
        sut.WithDetailTemplated(TemplateId).ShouldBeSameAs(sut);
        sut.WithDetailTemplated((string?)null).ShouldBeSameAs(sut);
        sut.WithInstanceUri(ErrorUri.Locator(LocatorUri)).ShouldBeSameAs(sut);
        sut.WithInstanceUri(null).ShouldBeSameAs(sut);
        sut.WithErrorDetail(new ErrorDetail(Pointer, Detail, null)).ShouldBeSameAs(sut);
        sut.WithErrorDetail(null).ShouldBeSameAs(sut);
        sut.WithErrorDetail(Pointer, Detail).ShouldBeSameAs(sut);
        sut.WithErrorDetail(null, null).ShouldBeSameAs(sut);
        sut.WithDetailParam(DefaultTemplateId, "limit", 100).ShouldBeSameAs(sut);
    }

    #endregion

    #region WithCategory

    [Theory]
    [InlineData(ErrorCategory.Validation)]
    [InlineData(ErrorCategory.Unauthenticated)]
    [InlineData(ErrorCategory.Unauthorized)]
    [InlineData(ErrorCategory.NotFound)]
    [InlineData(ErrorCategory.Timeout)]
    [InlineData(ErrorCategory.Conflict)]
    [InlineData(ErrorCategory.Failure)]
    [InlineData(ErrorCategory.Critical)]
    [InlineData(ErrorCategory.NotImplemented)]
    [InlineData(ErrorCategory.Unavailable)]
    public void WithCategory_AnyCategory_SetsCategory(ErrorCategory category)
    {
        // Arrange
        var initialCategory = category == ErrorCategory.Failure 
            ? ErrorCategory.Critical 
            : ErrorCategory.Failure;

        var sut = new ErrorBuilder(initialCategory, Title);

        // Act
        var error = sut
            .WithCategory(category)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Category.ShouldBe(category);
    }

    [Fact]
    public void WithCategory_CalledTwice_LastValueWins()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithCategory(ErrorCategory.Timeout)
            .WithCategory(ErrorCategory.NotFound)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Category.ShouldBe(ErrorCategory.NotFound);
    }

    #endregion

    #region WithTypeUri

    [Fact]
    public void WithTypeUri_Locator_SetsTypeUri()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithTypeUri(ErrorUri.Locator(LocatorUri))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.TypeUri.ShouldBe(LocatorUri);
    }

    [Fact]
    public void WithTypeUri_Tag_SetsTypeUri()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithTypeUri(ErrorUri.Tag(TagUri))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.TypeUri.ShouldBe(TagUri);
    }

    [Fact]
    public void WithTypeUri_NullWithoutPreviousValue_KeepsBlankTypeUri()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithTypeUri(null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.TypeUri.ShouldBe(BlankTypeUri);
    }

    [Fact]
    public void WithTypeUri_NullAfterValue_KeepsPreviousValue()
    {
        // Arrange
        var sut = CreateBuilder()
            .WithTypeUri(ErrorUri.Locator(LocatorUri));

        // Act
        var error = sut
            .WithTypeUri(null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.TypeUri.ShouldBe(LocatorUri);
    }

    [Fact]
    public void WithTypeUri_CalledTwice_LastValueWins()
    {
        // Arrange
        var sut = CreateBuilder()
            .WithTypeUri(ErrorUri.Locator(LocatorUri));

        // Act
        var error = sut
            .WithTypeUri(ErrorUri.Locator(OtherLocatorUri))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.TypeUri.ShouldBe(OtherLocatorUri);
    }

    #endregion

    #region WithTitle

    [Fact]
    public void WithTitle_TitleWithSurroundingWhitespace_SetsTrimmedTitle()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithTitle($"  {OtherTitle}  ")
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Title.ShouldBe(OtherTitle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithTitle_NullOrWhitespace_KeepsPreviousValue(string? title)
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithTitle(title)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Title.ShouldBe(Title);
    }

    [Fact]
    public void WithTitle_CalledTwice_LastValueWins()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithTitle("First title")
            .WithTitle(OtherTitle)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Title.ShouldBe(OtherTitle);
    }

    #endregion

    #region WithDetail

    [Fact]
    public void WithDetail_DetailWithSurroundingWhitespace_SetsTrimmedDetail()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetail($"  {Detail}  ")
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Detail.ShouldBe(Detail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithDetail_NullOrWhitespaceWithoutPreviousValue_KeepsNull(string? detail)
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetail(detail)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Detail.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithDetail_NullOrWhitespaceAfterValue_KeepsPreviousValue(string? detail)
    {
        // Arrange
        var sut = CreateBuilder()
            .WithDetail(Detail);

        // Act
        var error = sut
            .WithDetail(detail)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Detail.ShouldBe(Detail);
    }

    [Fact]
    public void WithDetail_CalledTwice_LastValueWins()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetail("First detail")
            .WithDetail(Detail)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.Detail.ShouldBe(Detail);
    }

    #endregion

    #region WithDetailTemplated (TemplatedMessage)

    [Fact]
    public void WithDetailTemplated_TemplatedMessage_SetsTemplateAndParams()
    {
        // Arrange
        var sut = CreateBuilder();
        var message = new TemplatedMessage(TemplateId, new Dictionary<string, object> { ["limit"] = 100 });

        // Act
        var error = sut
            .WithDetailTemplated(message)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailTemplated_NullTemplatedMessageWithoutPreviousValue_KeepsNull()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetailTemplated((TemplatedMessage?)null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldBeNull();
    }

    [Fact]
    public void WithDetailTemplated_NullTemplatedMessageAfterValue_KeepsPreviousValue()
    {
        // Arrange
        var message = new TemplatedMessage(TemplateId, new Dictionary<string, object> { ["limit"] = 100 });
        var sut = CreateBuilder()
            .WithDetailTemplated(message);

        // Act
        var error = sut
            .WithDetailTemplated((TemplatedMessage?)null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailTemplated_TemplatedMessageWithoutParams_UpdatesTemplateAndKeepsParams()
    {
        // Arrange
        var message = new TemplatedMessage(TemplateId, new Dictionary<string, object> { ["limit"] = 100 });
        var sut = CreateBuilder()
            .WithDetailTemplated(message);

        // Act
        var error = sut
            .WithDetailTemplated(new TemplatedMessage(OtherTemplateId))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailTemplated_TemplatedMessageCalledTwice_LastTemplateAndValuesWin()
    {
        // Arrange
        var firstMessage = new TemplatedMessage(TemplateId, new Dictionary<string, object> { ["limit"] = 100, ["window"] = "1m" });
        var secondMessage = new TemplatedMessage(OtherTemplateId, new Dictionary<string, object> { ["limit"] = 200 });
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetailTemplated(firstMessage)
            .WithDetailTemplated(secondMessage)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Count.ShouldBe(1);
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(200);
        error.DetailTemplated.Params.Keys.ShouldNotContain("window");
    }

    #endregion

    #region WithDetailTemplated (templateId, namedValues)

    [Fact]
    public void WithDetailTemplated_TemplateIdAndNamedValues_SetsTemplateAndParams()
    {
        // Arrange
        var sut = CreateBuilder();
        var namedValues = new Dictionary<string, object> { ["limit"] = 100 };

        // Act
        var error = sut
            .WithDetailTemplated(TemplateId, namedValues)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithDetailTemplated_NullOrWhitespaceTemplateId_KeepsTemplateAndAddsParams(string? templateId)
    {
        // Arrange
        var sut = CreateBuilder()
            .WithDetailTemplated(TemplateId);

        var namedValues = new Dictionary<string, object> { ["limit"] = 100 };

        // Act
        var error = sut
            .WithDetailTemplated(templateId, namedValues)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailTemplated_NamedValuesAfterTemplatedMessage_LastTemplateAndValuesWin()
    {
        // Arrange
        var message = new TemplatedMessage(TemplateId, new Dictionary<string, object> { ["limit"] = 100, ["window"] = "1m" });
        var namedValues = new Dictionary<string, object> { ["limit"] = 200 };
        var sut = CreateBuilder()
            .WithDetailTemplated(message);

        // Act
        var error = sut
            .WithDetailTemplated(OtherTemplateId, namedValues)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Count.ShouldBe(1);
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(200);
        error.DetailTemplated.Params.Keys.ShouldNotContain("window");
    }

    [Fact]
    public void WithDetailTemplated_TemplateIdAndNamedValuesCalledTwice_LastTemplateAndValuesWin()
    {
        // Arrange
        var firstValues = new Dictionary<string, object> { ["limit"] = 100, ["window"] = "1m" };
        var secondValues = new Dictionary<string, object> { ["limit"] = 200 };
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetailTemplated(TemplateId, firstValues)
            .WithDetailTemplated(OtherTemplateId, secondValues)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Count.ShouldBe(1);
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(200);
        error.DetailTemplated.Params.Keys.ShouldNotContain("window");
    }

    #endregion

    #region WithInstanceUri

    [Fact]
    public void WithInstanceUri_Locator_SetsInstanceUri()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithInstanceUri(ErrorUri.Locator(LocatorUri))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.InstanceUri.ShouldBe(LocatorUri);
    }

    [Fact]
    public void WithInstanceUri_Tag_SetsInstanceUri()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithInstanceUri(ErrorUri.Tag(TagUri))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.InstanceUri.ShouldBe(TagUri);
    }

    [Fact]
    public void WithInstanceUri_NullWithoutPreviousValue_KeepsNull()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithInstanceUri(null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.InstanceUri.ShouldBeNull();
    }

    [Fact]
    public void WithInstanceUri_NullAfterValue_KeepsPreviousValue()
    {
        // Arrange
        var sut = CreateBuilder()
            .WithInstanceUri(ErrorUri.Locator(LocatorUri));

        // Act
        var error = sut
            .WithInstanceUri(null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.InstanceUri.ShouldBe(LocatorUri);
    }

    [Fact]
    public void WithInstanceUri_CalledTwice_LastValueWins()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithInstanceUri(ErrorUri.Locator(LocatorUri))
            .WithInstanceUri(ErrorUri.Locator(OtherLocatorUri))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.InstanceUri.ShouldBe(OtherLocatorUri);
    }

    #endregion

    #region WithErrorDetail (ErrorDetail)

    [Fact]
    public void WithErrorDetail_ErrorDetail_AddsErrorDetail()
    {
        // Arrange
        var sut = CreateBuilder();
        var errorDetail = new ErrorDetail(Pointer, Detail, new TemplatedMessage(TemplateId));

        // Act
        var error = sut
            .WithErrorDetail(errorDetail)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].ShouldBe(errorDetail);
    }

    [Fact]
    public void WithErrorDetail_NullErrorDetail_DoesNotAddErrorDetail()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.ShouldBeEmpty();
    }

    #endregion

    #region WithErrorDetail (propertyPointer, detail, messageId, namedValues)

    [Fact]
    public void WithErrorDetail_PointerAndDetail_AddsErrorDetailWithoutTemplate()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(Pointer, Detail)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].PropertyPointer.ShouldBe(Pointer);
        error.ErrorDetails[0].Detail.ShouldBe(Detail);
        error.ErrorDetails[0].DetailTemplated.ShouldBeNull();
    }

    [Fact]
    public void WithErrorDetail_DetailWithSurroundingWhitespace_AddsTrimmedDetail()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(Pointer, $"  {Detail}  ")
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].Detail.ShouldBe(Detail);
    }

    [Fact]
    public void WithErrorDetail_PointerOnly_AddsErrorDetailWithEmptyDetail()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(Pointer, null)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].PropertyPointer.ShouldBe(Pointer);
        error.ErrorDetails[0].Detail.ShouldBe(string.Empty);
    }

    [Fact]
    public void WithErrorDetail_DetailOnly_AddsErrorDetailWithoutPointer()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(null, Detail)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].PropertyPointer.ShouldBeNull();
        error.ErrorDetails[0].Detail.ShouldBe(Detail);
    }

    [Fact]
    public void WithErrorDetail_MessageIdOnly_AddsErrorDetailWithTemplate()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(null, null, TemplateId)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].Detail.ShouldBe(string.Empty);

        var detailTemplated = error.ErrorDetails[0].DetailTemplated;
        detailTemplated.ShouldNotBeNull();
        detailTemplated.TemplateId.ShouldBe(TemplateId);
    }

    [Fact]
    public void WithErrorDetail_MessageIdAndNamedValues_AddsErrorDetailWithTemplateAndParams()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(Pointer, Detail, TemplateId, ("limit", 100), ("window", "1m"))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);

        var detailTemplated = error.ErrorDetails[0].DetailTemplated;
        detailTemplated.ShouldNotBeNull();
        detailTemplated.TemplateId.ShouldBe(TemplateId);
        detailTemplated.Params.ShouldNotBeNull();
        detailTemplated.Params.Keys.ShouldContain("limit");
        detailTemplated.Params["limit"].ShouldBe(100);
        detailTemplated.Params.Keys.ShouldContain("window");
        detailTemplated.Params["window"].ShouldBe("1m");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithErrorDetail_NullOrWhitespaceMessageIdWithDetail_AddsErrorDetailWithoutTemplate(string? messageId)
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(Pointer, Detail, messageId, ("limit", 100))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].Detail.ShouldBe(Detail);
        error.ErrorDetails[0].DetailTemplated.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", "")]
    [InlineData("   ", "   ", "   ")]
    public void WithErrorDetail_AllArgumentsNullOrWhitespace_DoesNotAddErrorDetail(string? propertyPointer, string? detail, string? messageId)
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(propertyPointer, detail, messageId)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.ShouldBeEmpty();
    }

    [Fact]
    public void WithErrorDetail_NamedValuesOnly_AddsErrorDetailWithoutTemplate()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(null, null, null, ("limit", 100))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].Detail.ShouldBe(string.Empty);
        error.ErrorDetails[0].DetailTemplated.ShouldBeNull();
    }

    [Fact]
    public void WithErrorDetail_CalledMultipleTimes_KeepsAllInOrder()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithErrorDetail(Pointer, Detail)
            .WithErrorDetail(new ErrorDetail(OtherPointer, "Other detail", null))
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.ErrorDetails.Count.ShouldBe(2);
        error.ErrorDetails[0].PropertyPointer.ShouldBe(Pointer);
        error.ErrorDetails[1].PropertyPointer.ShouldBe(OtherPointer);
    }

    #endregion

    #region WithDetailParam

    [Fact]
    public void WithDetailParam_NoTemplateSet_UsesDefaultTemplateAndAddsParam()
    {
        // Arrange
        var sut = CreateBuilder();

        // Act
        var error = sut
            .WithDetailParam(DefaultTemplateId, "limit", 100)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(DefaultTemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailParam_TemplateAlreadySet_KeepsTemplateAndExistingParams()
    {
        // Arrange
        var namedValues = new Dictionary<string, object> { ["window"] = "1m" };
        var sut = CreateBuilder()
            .WithDetailTemplated(TemplateId, namedValues);

        // Act
        var error = sut
            .WithDetailParam(DefaultTemplateId, "limit", 100)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("window");
        error.DetailTemplated.Params["window"].ShouldBe("1m");
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailParam_ParamAlreadySet_KeepsExistingValue()
    {
        // Arrange
        var namedValues = new Dictionary<string, object> { ["limit"] = 100 };
        var sut = CreateBuilder()
            .WithDetailTemplated(TemplateId, namedValues);

        // Act
        var error = sut
            .WithDetailParam(DefaultTemplateId, "limit", 200)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
    }

    [Fact]
    public void WithDetailParam_BeforeWithDetailTemplated_TemplateAndParamsFromWithDetailTemplatedWin()
    {
        // Arrange
        var namedValues = new Dictionary<string, object> { ["limit"] = 200 };
        var sut = CreateBuilder()
            .WithDetailParam(DefaultTemplateId, "limit", 100);

        // Act
        var error = sut
            .WithDetailTemplated(TemplateId, namedValues)
            .Build();

        // Assert
        error.ShouldNotBeNull();
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(200);
    }

    #endregion

    #region Build

    [Theory]
    [InlineData(ErrorCategory.Validation)]
    [InlineData(ErrorCategory.Unauthenticated)]
    [InlineData(ErrorCategory.Unauthorized)]
    [InlineData(ErrorCategory.NotFound)]
    [InlineData(ErrorCategory.Timeout)]
    [InlineData(ErrorCategory.Conflict)]
    [InlineData(ErrorCategory.Failure)]
    [InlineData(ErrorCategory.Critical)]
    [InlineData(ErrorCategory.NotImplemented)]
    [InlineData(ErrorCategory.Unavailable)]
    public void Build_AllPropertiesSet_MapsAllPropertiesForEachCategory(ErrorCategory category)
    {
        // Arrange
        var message = new TemplatedMessage(TemplateId, new Dictionary<string, object> { ["limit"] = 100 });
        var sut = new ErrorBuilder(category, Title)
            .WithTypeUri(ErrorUri.Locator(LocatorUri))
            .WithDetail(Detail)
            .WithDetailTemplated(message)
            .WithInstanceUri(ErrorUri.Locator(OtherLocatorUri))
            .WithErrorDetail(Pointer, Detail);

        // Act
        var error = sut.Build();

        // Assert
        error.ShouldNotBeNull();
        error.Category.ShouldBe(category);
        error.TypeUri.ShouldBe(LocatorUri);
        error.Title.ShouldBe(Title);
        error.Detail.ShouldBe(Detail);
        error.DetailTemplated.ShouldNotBeNull();
        error.DetailTemplated.TemplateId.ShouldBe(TemplateId);
        error.DetailTemplated.Params.ShouldNotBeNull();
        error.DetailTemplated.Params.Keys.ShouldContain("limit");
        error.DetailTemplated.Params["limit"].ShouldBe(100);
        error.InstanceUri.ShouldBe(OtherLocatorUri);
        error.ErrorDetails.Count.ShouldBe(1);
        error.ErrorDetails[0].PropertyPointer.ShouldBe(Pointer);
    }

    [Fact]
    public void Build_UnknownCategory_BuildsFailureError()
    {
        // Arrange
        var sut = new ErrorBuilder((ErrorCategory)999, Title);

        // Act
        var error = sut.Build();

        // Assert
        error.ShouldNotBeNull();
        error.Category.ShouldBe(ErrorCategory.Failure);
        error.Title.ShouldBe(Title);
    }

    #endregion

    #region WithDetailTemplated through ToResultAsync

    [Fact]
    public async Task WithDetailTemplated_NamedValuesReusedAcrossResponses_DoesNotModifyCallerDictionary()
    {
        // Arrange
        var sharedParams = new Dictionary<string, object> { ["limit"] = 100 };

        // Act
        var first = await CreateResponse(120)
            .ToResultAsync<object>((_, sut) => sut.WithDetailTemplated(TemplateId, sharedParams));

        var second = await CreateResponse(5)
            .ToResultAsync<object>((_, sut) => sut.WithDetailTemplated(TemplateId, sharedParams));

        var third = await CreateResponse()
            .ToResultAsync<object>((_, sut) => sut.WithDetailTemplated(TemplateId, sharedParams));

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
        var first = await CreateResponse(120)
            .ToResultAsync<object>((_, sut) => sut.WithDetailTemplated(sharedMessage));

        var second = await CreateResponse(5)
            .ToResultAsync<object>((_, sut) => sut.WithDetailTemplated(sharedMessage));

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
        var result = await CreateResponse()
            .ToResultAsync<object>((_, sut) =>
                sut
                    .WithDetailTemplated(message)
                    .WithDetailTemplated(OtherTemplateId, namedValues));

        // Assert
        messageParams.Count.ShouldBe(1);
        messageParams.Keys.ShouldNotContain("window");

        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.TemplateId.ShouldBe(OtherTemplateId);
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Keys.ShouldNotContain("limit");
        result.Error.DetailTemplated.Params.Keys.ShouldContain("window");
        result.Error.DetailTemplated.Params["window"].ShouldBe("1m");
    }

    [Fact]
    public async Task WithDetailTemplated_NamedValuesCalledTwice_LastValuesWin()
    {
        // Arrange
        var firstValues = new Dictionary<string, object> { ["limit"] = 100, ["window"] = "1m" };
        var secondValues = new Dictionary<string, object> { ["limit"] = 200 };

        // Act
        var result = await CreateResponse()
            .ToResultAsync<object>((_, sut) =>
                sut
                    .WithDetailTemplated(TemplateId, firstValues)
                    .WithDetailTemplated(TemplateId, secondValues));

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess().ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error.DetailTemplated.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.ShouldNotBeNull();
        result.Error.DetailTemplated.Params.Count.ShouldBe(1);
        result.Error.DetailTemplated.Params.Keys.ShouldContain("limit");
        result.Error.DetailTemplated.Params["limit"].ShouldBe(200);
        result.Error.DetailTemplated.Params.Keys.ShouldNotContain("window");
    }

    [Fact]
    public async Task WithDetailTemplated_NullNamedValues_KeepsExistingParams()
    {
        // Arrange
        var namedValues = new Dictionary<string, object> { ["limit"] = 100 };

        // Act
        var result = await CreateResponse()
            .ToResultAsync<object>((_, sut) =>
                sut
                    .WithDetailTemplated(TemplateId, namedValues)
                    .WithDetailTemplated(OtherTemplateId));

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
        var result = await CreateResponse()
            .ToResultAsync<object>((_, sut) => sut.WithDetailTemplated(TemplateId, namedValues));

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

    #endregion

    #region helpers

    private static ErrorBuilder CreateBuilder()
    {
        return new ErrorBuilder(ErrorCategory.Failure, Title);
    }

    private static HttpResponseMessage CreateResponse(int? retryAfterSeconds = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        if (retryAfterSeconds is not null)
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(retryAfterSeconds.Value));

        return response;
    }

    #endregion
}
