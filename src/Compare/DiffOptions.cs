/*
* Diff Match and Patch
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
* Copyright 2024 Andrew Chisholm.
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
/// <param name="CheckLines">Speedup flag. If false, then don't run a
/// line-level diff first to identify the changed areas.
/// If true, then run a faster slightly less optimal diff.</param>
public record class DiffOptions(float TimeoutSeconds, short EditCost, bool CheckLines)
{
    /// <summary>
    /// Slightly faster line level diff.
    /// </summary>
    public static readonly DiffOptions Default = new(0.5f, 4, true);

    /// <summary>
    /// Slower more accurate diff settings, where diffs can be at the character level.
    /// </summary>
    public static readonly DiffOptions Accurate = new(0.5f, 4, false);

    public CancellationTimer StartTimer() => new CancellationTimer(this.TimeoutSeconds);

    public sealed class CancellationTimer : IDisposable
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
