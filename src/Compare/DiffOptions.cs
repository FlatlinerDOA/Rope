/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
*
* Copyright 2024 Andrew Chisholm (FlatlinerDOA).
* https://github.com/FlatlinerDOA/Rope
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
* 
*/

namespace Rope.Compare;

using System;

/// <summary>
/// Configuration options for a diff (Immutable).
/// You can make changes using with operator.
/// Example: DiffOptions.Default with { TimeoutSeconds = 0.2f }.
/// </summary>
/// <param name="TimeoutSeconds">The time in seconds to allow the diff to calculate for.</param>
/// <param name="EditCost">The minimum text length for a Diff operation, larger values result in more chunky diff operations.</param>
/// <param name="IsChunkingEnabled">Speedup flag. If false, then don't run a
/// line-level diff first to identify the changed areas.
/// If true, then run a faster slightly less optimal diff.</param>
/// <param name="ChunkSeparator">An element that delineates one chunk from the next, the diff will attempt to use this if chunkng is enabled.</param>
public record class DiffOptions<T>(float TimeoutSeconds, short EditCost, bool IsChunkingEnabled, ReadOnlyMemory<T> ChunkSeparator) where T : IEquatable<T>
{
    /// <summary>
    /// Slower more accurate diff settings, where diffs can be down to the element level. (Timeout default is 500ms)
    /// </summary>
    public static readonly DiffOptions<T> Generic = new(0.5f, 4, false, ReadOnlyMemory<T>.Empty);

    /// <summary>
    /// Slightly faster line level diff on chars only. (Timeout default is 500ms)
    /// </summary>
    public static readonly DiffOptions<char> LineLevel = new(0.5f, 4, true, new[] { '\n' });

    /// <summary>
    /// Gets the defaults options to use.
    /// Uses <see cref="DiffOptions{char}.LineLevel"/> when <typeparamref name="T"/> is <see cref="char"/>,
    /// otherwise uses <see cref="DiffOptions{char}.Generic"/>.
    /// </summary>
    public static DiffOptions<T> Default => typeof(T) == typeof(char) ?
            (DiffOptions<char>.LineLevel as DiffOptions<T>)! :
            DiffOptions<T>.Generic;

    internal CancellationTimer StartTimer() => new CancellationTimer(this.TimeoutSeconds);

    public DiffOptions<T> WithChunking(bool isChunkingEnabled) => this.IsChunkingEnabled == isChunkingEnabled ?
        this :
        this with { IsChunkingEnabled = isChunkingEnabled };

    internal sealed class CancellationTimer : IDisposable
    {
        private readonly CancellationTokenSource source;

        public CancellationTimer(float timeoutSeconds) : this(timeoutSeconds <= 0.0f ? TimeSpan.Zero : TimeSpan.FromSeconds(timeoutSeconds))
        {
        }

        public CancellationTimer(TimeSpan timeout)
        {
            this.source = new CancellationTokenSource();
            if (timeout != TimeSpan.Zero)
            {
                this.source.CancelAfter(timeout);
            }
        }

        public CancellationToken Cancellation => this.source.Token;

        public void Dispose()
        {
            this.source.Dispose();
        }
    }
}
