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

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Fakes;

/// <summary>
/// A content that stops with an OperationCanceledException when the token that it is read with is canceled, so that a
/// test sees whether the token of the caller reaches the read.
/// </summary>
internal sealed class CancellationObservingHttpContent : HttpContent
{
    private readonly byte[] _body;

    public CancellationObservingHttpContent(string body, string contentType = "application/json")
    {
        _body = Encoding.UTF8.GetBytes(body);
        Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
    }

    protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new MemoryStream(_body);

        return Task.FromResult(stream);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await stream.WriteAsync(_body, cancellationToken);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _body.Length;

        return true;
    }
}
