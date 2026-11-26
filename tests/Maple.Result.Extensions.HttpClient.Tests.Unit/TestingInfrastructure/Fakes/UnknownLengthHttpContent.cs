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

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.Fakes;

/// <summary>
/// A content without a Content-Length header, as the body of a chunked response: whether it is empty is known only after it is read.
/// </summary>
internal sealed class UnknownLengthHttpContent : HttpContent
{
    private readonly byte[] _body;

    public UnknownLengthHttpContent(byte[] body, string? contentType)
    {
        _body = body;

        if (contentType is not null)
            Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return stream.WriteAsync(_body, 0, _body.Length);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;

        return false;
    }
}
