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
/// A content whose read completes later, on another thread, as the read of a network stream does: the code that awaits it
/// has to continue somewhere.
/// </summary>
/// <remarks>
/// The fake itself does not use the synchronization context of the caller, so a test sees only what the awaiting code does.
/// </remarks>
internal sealed class AsynchronousHttpContent : HttpContent
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(20);

    private readonly byte[] _body;

    public AsynchronousHttpContent(string body, string contentType = "application/json")
    {
        _body = Encoding.UTF8.GetBytes(body);
        Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
    }

    protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

        return new MemoryStream(_body);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(_body, cancellationToken).ConfigureAwait(false);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _body.Length;

        return true;
    }
}
