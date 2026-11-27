// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Results.Extensions.HttpClient.Helpers;
using System.Text.Json;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.Helpers;

/// <summary>
/// The values of the extension members and of the template parameters: plain .NET values, because <c>TryGetMetadata&lt;T&gt;</c> does
/// not convert and a <see cref="JsonElement" /> cannot be stored by the serializer packages.
/// </summary>
public class JsonValueConverterUnitTests
{
    #region ConvertToValue

    [Fact]
    public void ConvertToValue_String_ReturnsTheString()
    {
        // Act
        var result = Convert("\"a\\u00e9b\"");

        // Assert
        result.ShouldBeOfType<string>();
        result.ShouldBe("aéb");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ConvertToValue_Boolean_ReturnsABool(string json, bool expected)
    {
        // Act
        var result = Convert(json);

        // Assert
        result.ShouldBeOfType<bool>();
        result.ShouldBe(expected);
    }

    [Fact]
    public void ConvertToValue_Null_ReturnsNull()
    {
        // Act
        var result = Convert("null");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("42", 42)]
    [InlineData("-7", -7)]
    [InlineData("2147483647", int.MaxValue)]
    [InlineData("-2147483648", int.MinValue)]
    public void ConvertToValue_IntegerInTheRangeOfAnInt_ReturnsAnInt(string json, int expected)
    {
        // Act
        var result = Convert(json);

        // Assert
        result.ShouldBeOfType<int>();
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("2147483648", 2147483648L)]
    [InlineData("-3000000000", -3000000000L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    public void ConvertToValue_IntegerInTheRangeOfALong_ReturnsALong(string json, long expected)
    {
        // Act
        var result = Convert(json);

        // Assert
        result.ShouldBeOfType<long>();
        result.ShouldBe(expected);
    }

    [Fact]
    public void ConvertToValue_IntegerOutOfTheRangeOfALong_ReturnsADecimal()
    {
        // Act
        var result = Convert("100000000000000000000");

        // Assert
        result.ShouldBeOfType<decimal>();
        result.ShouldBe(100000000000000000000m);
    }

    [Fact]
    public void ConvertToValue_IntegerOutOfTheRangeOfADecimal_ReturnsADouble()
    {
        // Act
        var result = Convert("1" + new string('0', 40));

        // Assert
        result.ShouldBeOfType<double>();
        result.ShouldBe(1e40);
    }

    [Theory]
    [InlineData("1.5", "1.5")]
    [InlineData("-0.25", "-0.25")]
    [InlineData("1.0", "1.0")]
    [InlineData("1e3", "1000")]
    [InlineData("2.5E-2", "0.025")]
    public void ConvertToValue_NumberWithAFractionOrAnExponent_ReturnsADecimal(string json, string expected)
    {
        // Act
        var result = Convert(json);

        // Assert
        result.ShouldBeOfType<decimal>();
        result.ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertToValue_NumberWithAFractionOutOfTheRangeOfADecimal_ReturnsADouble()
    {
        // Act
        var result = Convert("1.5e30");

        // Assert
        result.ShouldBeOfType<double>();
        result.ShouldBe(1.5e30);
    }

    // A number that not even a double holds (it would be infinity, which JSON cannot carry) is kept as its text.
    [Theory]
    [InlineData("1e400")]
    [InlineData("-1e400")]
    public void ConvertToValue_NumberOutOfTheRangeOfADouble_ReturnsItsText(string json)
    {
        // Act
        var result = Convert(json);

        // Assert
        result.ShouldBeOfType<string>();
        result.ShouldBe(json);
    }

    [Fact]
    public void ConvertToValue_Array_ReturnsAnArrayOfTheValues()
    {
        // Act
        var result = Convert("""[1, "a", null, true, [2]]""");

        // Assert
        var array = result.ShouldBeOfType<object?[]>();
        array.Length.ShouldBe(5);
        array[0].ShouldBe(1);
        array[1].ShouldBe("a");
        array[2].ShouldBeNull();
        array[3].ShouldBe(true);
        var innerArray = array[4].ShouldBeOfType<object?[]>();
        innerArray.ShouldBe(new object?[] { 2 });
    }

    [Fact]
    public void ConvertToValue_EmptyArray_ReturnsAnEmptyArray()
    {
        // Act
        var result = Convert("[]");

        // Assert
        var array = result.ShouldBeOfType<object?[]>();
        array.ShouldBeEmpty();
    }

    [Fact]
    public void ConvertToValue_Object_ReturnsADictionaryOfTheValues()
    {
        // Act
        var result = Convert("""{"name": "Ada", "age": 36, "tags": ["a"], "address": {"city": "London"}}""");

        // Assert
        var dictionary = result.ShouldBeOfType<Dictionary<string, object?>>();
        dictionary.Count.ShouldBe(4);
        dictionary.Keys.ShouldContain("name");
        dictionary["name"].ShouldBe("Ada");
        dictionary.Keys.ShouldContain("age");
        dictionary["age"].ShouldBe(36);
        dictionary.Keys.ShouldContain("tags");
        var tags = dictionary["tags"].ShouldBeOfType<object?[]>();
        tags.ShouldBe(new object?[] { "a" });
        dictionary.Keys.ShouldContain("address");
        var address = dictionary["address"].ShouldBeOfType<Dictionary<string, object?>>();
        address.Keys.ShouldContain("city");
        address["city"].ShouldBe("London");
    }

    // The names of an object are compared with their spelling: only an exact repeat is the same member, and the last one wins.
    [Fact]
    public void ConvertToValue_ObjectWithARepeatedName_KeepsTheLastValue()
    {
        // Act
        var result = Convert("""{"a": 1, "A": 2, "a": 3}""");

        // Assert
        var dictionary = result.ShouldBeOfType<Dictionary<string, object?>>();
        dictionary.Count.ShouldBe(2);
        dictionary.Keys.ShouldContain("a");
        dictionary["a"].ShouldBe(3);
        dictionary.Keys.ShouldContain("A");
        dictionary["A"].ShouldBe(2);
    }

    #endregion

    #region helper methods

    private static object? Convert(string json)
    {
        using var document = JsonDocument.Parse(json);

        return JsonValueConverter.ConvertToValue(document.RootElement);
    }

    #endregion
}
