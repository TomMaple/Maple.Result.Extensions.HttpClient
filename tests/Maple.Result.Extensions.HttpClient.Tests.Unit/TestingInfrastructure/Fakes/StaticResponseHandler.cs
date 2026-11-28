// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Fakes;

/// <summary>
/// A primary handler of an <see cref="System.Net.Http.HttpClient" /> that answers every request with a new response from the
/// factory, so the response has its request, as one received from a server.
/// </summary>
internal sealed class StaticResponseHandler(Func<HttpResponseMessage> createResponse) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = createResponse();
        response.RequestMessage = request;

        return Task.FromResult(response);
    }
}
