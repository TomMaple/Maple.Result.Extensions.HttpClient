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
/// A content whose headers are there but whose body cannot be read, as when a connection is dropped after the headers.
/// </summary>
/// <remarks>
/// It fails as the content of a real response does: read as a stream, the stream throws the exception as it is; read as
/// text or bytes, HttpContent copies the body into its buffer first and wraps an IOException in an HttpRequestException.
/// </remarks>
internal sealed class ThrowingHttpContent : HttpContent
{
    private readonly Exception _exception;

    public ThrowingHttpContent(Exception exception, string? contentType = "application/json")
    {
        _exception = exception;

        if (contentType is not null)
            Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
    }

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        Stream stream = new ThrowingStream(_exception);

        return Task.FromResult(stream);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return Task.FromException(_exception);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;

        return false;
    }

    private sealed class ThrowingStream : Stream
    {
        private readonly Exception _exception;

        public ThrowingStream(Exception exception)
        {
            _exception = exception;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw _exception;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
