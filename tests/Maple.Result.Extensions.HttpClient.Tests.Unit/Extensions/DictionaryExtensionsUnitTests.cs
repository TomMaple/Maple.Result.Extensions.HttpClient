using System.Globalization;
using Maple.Result.Extensions.HttpClient.Extensions;

namespace Maple.Result.Extensions.HttpClient.Tests.Unit.Extensions;

public class DictionaryExtensionsUnitTests
{
    #region consts

    private const string Key = "detailTemplated";
    private const string Value = "some value";

    #endregion

    [Fact]
    public void GetValueOrNull_NullDictionary_ReturnsNull()
    {
        // Arrange
        IDictionary<string, object?>? dictionary = null;

        // Act
        var result = dictionary.GetValueOrNull(Key);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetValueOrNull_EmptyDictionary_ReturnsNull()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>();

        // Act
        var result = dictionary.GetValueOrNull(Key);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetValueOrNull_MissingKey_ReturnsNull()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { ["other"] = Value };

        // Act
        var result = dictionary.GetValueOrNull(Key);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetValueOrNull_ExistingKey_ReturnsValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { ["other"] = "other value", [Key] = Value };

        // Act
        var result = dictionary.GetValueOrNull(Key);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(Value);
    }

    [Fact]
    public void GetValueOrNull_ExistingKey_ReturnsSameInstance()
    {
        // Arrange
        var value = new object();
        var dictionary = new Dictionary<string, object?> { [Key] = value };

        // Act
        var result = dictionary.GetValueOrNull(Key);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(value);
    }

    [Fact]
    public void GetValueOrNull_ExistingKeyWithNullValue_ReturnsNull()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { [Key] = null };

        // Act
        var result = dictionary.GetValueOrNull(Key);

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("detailtemplated")]
    [InlineData("DETAILTEMPLATED")]
    [InlineData("DetailTemplated")]
    [InlineData("dEtAiLtEmPlAtEd")]
    public void GetValueOrNull_KeyDiffersByCase_ReturnsValue(string key)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { [Key] = Value };

        // Act
        var result = dictionary.GetValueOrNull(key);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(Value);
    }

    [Fact]
    public void GetValueOrNull_KeyDiffersByCaseUnderTurkishCulture_ReturnsValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { ["title"] = Value };
        var originalCulture = CultureInfo.CurrentCulture;
        object? result;

        // Act
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            result = dictionary.GetValueOrNull("TITLE");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(Value);
    }

    [Fact]
    public void GetValueOrNull_EmptyStringKey_ReturnsValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { [string.Empty] = Value };

        // Act
        var result = dictionary.GetValueOrNull(string.Empty);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(Value);
    }

    [Fact]
    public void GetValueOrNull_OtherDictionaryImplementation_ReturnsValue()
    {
        // Arrange
        IDictionary<string, object?> dictionary = new SortedDictionary<string, object?> { [Key] = Value };

        // Act
        var result = dictionary.GetValueOrNull("DETAILTEMPLATED");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(Value);
    }
}
