// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Net;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit;

public class HttpErrorContextUnitTests
{
    private const string Content = """{"message":"Not here."}""";
    private const string DefaultTitle = "Not Found";

    #region constructor

    [Fact]
    public void Ctor_Values_AreReturnedByTheProperties()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        // Act
        var sut = new HttpErrorContext(response, Content, ErrorCategory.NotFound, DefaultTitle);

        // Assert
        sut.Response.ShouldBeSameAs(response);
        sut.Content.ShouldBe(Content);
        sut.DefaultCategory.ShouldBe(ErrorCategory.NotFound);
        sut.DefaultTitle.ShouldBe(DefaultTitle);
    }

    #endregion

    #region RequestUri

    [Fact]
    public void RequestUri_ResponseOfARequest_ReturnsTheUriOfTheRequest()
    {
        // Arrange
        var requestUri = new Uri("https://orders.example.com/orders/42");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        response.RequestMessage = request;
        var sut = new HttpErrorContext(response, Content, ErrorCategory.NotFound, DefaultTitle);

        // Act
        var result = sut.RequestUri;

        // Assert
        result.ShouldBe(requestUri);
    }

    [Fact]
    public void RequestUri_ResponseWithoutARequest_ReturnsNull()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var sut = new HttpErrorContext(response, Content, ErrorCategory.NotFound, DefaultTitle);

        // Act
        var result = sut.RequestUri;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void RequestUri_RequestWithoutAUri_ReturnsNull()
    {
        // Arrange
        using var request = new HttpRequestMessage();
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        response.RequestMessage = request;
        var sut = new HttpErrorContext(response, Content, ErrorCategory.NotFound, DefaultTitle);

        // Act
        var result = sut.RequestUri;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region StatusCode

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData((HttpStatusCode)599)]
    public void StatusCode_Response_ReturnsTheStatusCodeOfTheResponse(HttpStatusCode statusCode)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode);
        var sut = new HttpErrorContext(response, Content, ErrorCategory.Failure, DefaultTitle);

        // Act
        var result = sut.StatusCode;

        // Assert
        result.ShouldBe(statusCode);
    }

    #endregion
}
