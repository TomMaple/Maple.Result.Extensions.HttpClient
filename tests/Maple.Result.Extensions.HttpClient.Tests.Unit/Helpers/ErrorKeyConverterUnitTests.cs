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
/// The keys of the errors of an ASP.NET Core ValidationProblemDetails body, converted to JSON Pointers (RFC 6901).
/// </summary>
public class ErrorKeyConverterUnitTests
{
    #region ConvertToPointer

    // The keys of the model state of ASP.NET Core and the paths of the binding errors of System.Text.Json.
    [Theory]
    [InlineData("Email", "/Email")]
    [InlineData("Address.Street", "/Address/Street")]
    [InlineData("Items[0].Name", "/Items/0/Name")]
    [InlineData("Items[0]", "/Items/0")]
    [InlineData("Items[12][3]", "/Items/12/3")]
    [InlineData("[0].Name", "/0/Name")]
    [InlineData("$.items[0].name", "/items/0/name")]
    [InlineData("$.email", "/email")]
    [InlineData("$[1]", "/1")]
    [InlineData("$['first name']", "/first name")]
    [InlineData("$.order['a.b']", "/order/a.b")]
    [InlineData("first name", "/first name")]
    public void ConvertToPointer_Key_ReturnsThePointer(string key, string expected)
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer(key, null);

        // Assert
        result.ShouldBe(expected);
    }

    // The root of the input: an error of the whole body or of the model.
    [Theory]
    [InlineData("")]
    [InlineData("$")]
    public void ConvertToPointer_KeyOfTheRoot_ReturnsTheEmptyPointer(string key)
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer(key, null);

        // Assert
        result.ShouldBe(string.Empty);
    }

    // RFC 6901, section 3: ~ is written as ~0 and / as ~1, so a name cannot give a wrong pointer.
    [Theory]
    [InlineData("a/b", "/a~1b")]
    [InlineData("a~b", "/a~0b")]
    [InlineData("$['a/b']", "/a~1b")]
    public void ConvertToPointer_NameWithTildeOrSlash_EscapesThem(string key, string expected)
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer(key, null);

        // Assert
        result.ShouldBe(expected);
    }

    // A key that is not in the syntax of ASP.NET Core or of System.Text.Json is one name.
    [Theory]
    [InlineData("Items[")]
    [InlineData("Items[x].Name")]
    [InlineData("Items[-1]")]
    [InlineData("Items[0]Name")]
    [InlineData("Items.")]
    [InlineData("a..b")]
    [InlineData("$foo")]
    [InlineData(".Email")]
    [InlineData("$['open")]
    [InlineData("Items[99999999999999999999]")]
    public void ConvertToPointer_KeyInAnotherSyntax_ReturnsOneName(string key)
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer(key, null);

        // Assert
        result.ShouldBe("/" + key);
    }

    [Theory]
    [InlineData("Email", "/email")]
    [InlineData("Items[0].Name", "/items/0/name")]
    [InlineData("$.Items[0].FirstName", "/items/0/firstName")]
    [InlineData("Address.Street", "/address/street")]
    [InlineData("$['First Name']", "/first Name")]
    public void ConvertToPointer_NamingPolicy_ConvertsEachName(string key, string expected)
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer(key, JsonNamingPolicy.CamelCase);

        // Assert
        result.ShouldBe(expected);
    }

    // The policy changes the names, never an index.
    [Fact]
    public void ConvertToPointer_NamingPolicyAndAnIndex_KeepsTheIndex()
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer("OrderItems[10].UnitPrice", JsonNamingPolicy.SnakeCaseLower);

        // Assert
        result.ShouldBe("/order_items/10/unit_price");
    }

    [Fact]
    public void ConvertToPointer_NamingPolicyAndAKeyInAnotherSyntax_ConvertsTheKeyAsOneName()
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer("Items[x]", JsonNamingPolicy.CamelCase);

        // Assert
        result.ShouldBe("/items[x]");
    }

    [Theory]
    [InlineData("")]
    [InlineData("$")]
    public void ConvertToPointer_NamingPolicyAndTheKeyOfTheRoot_ReturnsTheEmptyPointer(string key)
    {
        // Act
        var result = ErrorKeyConverter.ConvertToPointer(key, JsonNamingPolicy.CamelCase);

        // Assert
        result.ShouldBe(string.Empty);
    }

    // A naming policy is code of the caller: an exception that it throws is not caught.
    [Fact]
    public void ConvertToPointer_NamingPolicyThrows_ThrowsTheSameException()
    {
        // Arrange
        var namingPolicy = new ThrowingNamingPolicy();

        // Act
        var exception = Record.Exception(() => ErrorKeyConverter.ConvertToPointer("Email", namingPolicy));

        // Assert
        exception.ShouldBeSameAs(namingPolicy.Exception);
    }

    #endregion

    #region helper classes

    private sealed class ThrowingNamingPolicy : JsonNamingPolicy
    {
        internal InvalidOperationException Exception { get; } = new("The policy failed.");

        public override string ConvertName(string name)
        {
            throw Exception;
        }
    }

    #endregion
}
