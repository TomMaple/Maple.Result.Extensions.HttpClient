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
using System.Net.Http.Headers;
using System.Text;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Helpers;

/// <summary>
/// Creates the responses that the tests map.
/// </summary>
internal static class ResponseFactory
{
    /// <summary>
    /// A response without a body.
    /// </summary>
    internal static HttpResponseMessage Create(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode);
    }

    /// <summary>
    /// A response with the given content.
    /// </summary>
    internal static HttpResponseMessage Create(HttpStatusCode statusCode, HttpContent content)
    {
        return new HttpResponseMessage(statusCode) { Content = content };
    }

    /// <summary>
    /// A response with a body in UTF-8 and the Content-Type header as given, e.g., "application/json; charset=utf-8";
    /// without a Content-Type header when it is null.
    /// </summary>
    internal static HttpResponseMessage Create(HttpStatusCode statusCode, string body, string? contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(body);

        return Create(statusCode, bytes, contentType);
    }

    /// <summary>
    /// A response with the given bytes as its body and the Content-Type header as given.
    /// </summary>
    internal static HttpResponseMessage Create(HttpStatusCode statusCode, byte[] body, string? contentType)
    {
        var content = new ByteArrayContent(body);
        if (contentType is not null)
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        return Create(statusCode, content);
    }

    /// <summary>
    /// A response with a JSON body (application/json, UTF-8).
    /// </summary>
    internal static HttpResponseMessage CreateJson(HttpStatusCode statusCode, string json)
    {
        return Create(statusCode, json, "application/json; charset=utf-8");
    }

    /// <summary>
    /// A response with a problem details body (application/problem+json, UTF-8), and the request that it answers when a request URI
    /// is given.
    /// </summary>
    internal static HttpResponseMessage CreateProblemDetails(HttpStatusCode statusCode, string json, string? requestUri = null)
    {
        var response = Create(statusCode, json, "application/problem+json; charset=utf-8");
        if (requestUri is not null)
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

        return response;
    }
}
