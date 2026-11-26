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
using Maple.Results.Http;
using System.Net;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.Helpers;

public class HttpStatusPhrasesUnitTests
{
    #region Get

    // The phrases of RFC 9110 and of the HTTP status code registry of IANA.
    [Theory]
    [InlineData(100, "Continue")]
    [InlineData(101, "Switching Protocols")]
    [InlineData(102, "Processing")]
    [InlineData(103, "Early Hints")]
    [InlineData(200, "OK")]
    [InlineData(201, "Created")]
    [InlineData(202, "Accepted")]
    [InlineData(203, "Non-Authoritative Information")]
    [InlineData(204, "No Content")]
    [InlineData(205, "Reset Content")]
    [InlineData(206, "Partial Content")]
    [InlineData(207, "Multi-Status")]
    [InlineData(208, "Already Reported")]
    [InlineData(226, "IM Used")]
    [InlineData(300, "Multiple Choices")]
    [InlineData(301, "Moved Permanently")]
    [InlineData(302, "Found")]
    [InlineData(303, "See Other")]
    [InlineData(304, "Not Modified")]
    [InlineData(305, "Use Proxy")]
    [InlineData(307, "Temporary Redirect")]
    [InlineData(308, "Permanent Redirect")]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(402, "Payment Required")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(405, "Method Not Allowed")]
    [InlineData(406, "Not Acceptable")]
    [InlineData(407, "Proxy Authentication Required")]
    [InlineData(408, "Request Timeout")]
    [InlineData(409, "Conflict")]
    [InlineData(410, "Gone")]
    [InlineData(411, "Length Required")]
    [InlineData(412, "Precondition Failed")]
    [InlineData(413, "Content Too Large")]
    [InlineData(414, "URI Too Long")]
    [InlineData(415, "Unsupported Media Type")]
    [InlineData(416, "Range Not Satisfiable")]
    [InlineData(417, "Expectation Failed")]
    [InlineData(421, "Misdirected Request")]
    [InlineData(422, "Unprocessable Content")]
    [InlineData(423, "Locked")]
    [InlineData(424, "Failed Dependency")]
    [InlineData(425, "Too Early")]
    [InlineData(426, "Upgrade Required")]
    [InlineData(428, "Precondition Required")]
    [InlineData(429, "Too Many Requests")]
    [InlineData(431, "Request Header Fields Too Large")]
    [InlineData(451, "Unavailable For Legal Reasons")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(501, "Not Implemented")]
    [InlineData(502, "Bad Gateway")]
    [InlineData(503, "Service Unavailable")]
    [InlineData(504, "Gateway Timeout")]
    [InlineData(505, "HTTP Version Not Supported")]
    [InlineData(506, "Variant Also Negotiates")]
    [InlineData(507, "Insufficient Storage")]
    [InlineData(508, "Loop Detected")]
    [InlineData(510, "Not Extended")]
    [InlineData(511, "Network Authentication Required")]
    public void Get_RegisteredStatusCode_ReturnsItsPhrase(int statusCode, string expected)
    {
        // Act
        var result = HttpStatusPhrases.Get((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe(expected);
    }

    // Codes that are not registered (306 and 418 are reserved as unused), and numbers outside the classes of HTTP.
    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(104)]
    [InlineData(299)]
    [InlineData(306)]
    [InlineData(418)]
    [InlineData(419)]
    [InlineData(499)]
    [InlineData(509)]
    [InlineData(599)]
    [InlineData(600)]
    [InlineData(999)]
    public void Get_UnregisteredStatusCode_ReturnsUnknownError(int statusCode)
    {
        // Act
        var result = HttpStatusPhrases.Get((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe("Unknown Error");
    }

    // The titles that the packages of Maple.Result write for an error without a type of its own are read back unchanged.
    public static TheoryData<int, string> PhrasesOfTheCore
        => new()
        {
            { 400, HttpErrorTitles.Validation },
            { 401, HttpErrorTitles.Unauthenticated },
            { 403, HttpErrorTitles.PermissionDenied },
            { 404, HttpErrorTitles.NotFound },
            { 408, HttpErrorTitles.Timeout },
            { 409, HttpErrorTitles.Conflict },
            { 422, HttpErrorTitles.Failure },
            { 429, HttpErrorTitles.LimitExceeded },
            { 500, HttpErrorTitles.Critical },
            { 501, HttpErrorTitles.NotImplemented },
            { 503, HttpErrorTitles.Unavailable },
        };

    [Theory]
    [MemberData(nameof(PhrasesOfTheCore))]
    public void Get_StatusCodeThatACategoryWrites_ReturnsTheTitleOfTheCore(int statusCode, string expected)
    {
        // Act
        var result = HttpStatusPhrases.Get((HttpStatusCode)statusCode);

        // Assert
        result.ShouldBe(expected);
    }

    #endregion
}
