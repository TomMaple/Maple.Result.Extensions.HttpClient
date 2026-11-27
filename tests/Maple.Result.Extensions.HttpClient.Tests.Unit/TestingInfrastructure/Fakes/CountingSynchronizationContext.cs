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
/// A synchronization context that counts the continuations posted to it, as the context of a user interface thread gets them
/// from code that awaits without ConfigureAwait(false).
/// </summary>
internal sealed class CountingSynchronizationContext : SynchronizationContext
{
    private int _posts;

    /// <summary>
    /// The number of continuations that were posted or sent to the context.
    /// </summary>
    public int Posts => Volatile.Read(ref _posts);

    public override void Post(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        ThreadPool.QueueUserWorkItem(_ => d(state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        Interlocked.Increment(ref _posts);
        d(state);
    }
}
