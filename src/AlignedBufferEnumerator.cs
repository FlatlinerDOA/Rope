// Copyright 2024 Andrew Chisholm (https://github.com/FlatlinerDOA)

namespace Rope;

using System;

/// <summary>
/// Enumerates over two buffers, slicing them into even length spans so they are aligned.
/// </summary>
/// <typeparam name="T">The type of items in the buffers.</typeparam>
public ref struct AlignedBufferEnumerator<T>
{
    private ReadOnlySpan<ReadOnlyMemory<T>> aBuffers;
    private ReadOnlySpan<ReadOnlyMemory<T>> bBuffers;

    /// <summary>
    /// Current buffer A index;
    /// </summary>
    private int currentAIndex;
    private int currentBIndex;

    private ReadOnlySpan<T> currentASpan;
    private ReadOnlySpan<T> currentBSpan;

    private int offsetA;
    private int offsetB;

    public AlignedBufferEnumerator(ReadOnlySpan<ReadOnlyMemory<T>> aBuffers, ReadOnlySpan<ReadOnlyMemory<T>> bBuffers) : this(aBuffers[0].Span, bBuffers[0].Span, aBuffers, bBuffers)
    {
    }

    public AlignedBufferEnumerator(ReadOnlySpan<T> currentASpan, ReadOnlySpan<T> currentBSpan, ReadOnlySpan<ReadOnlyMemory<T>> aBuffers, ReadOnlySpan<ReadOnlyMemory<T>> bBuffers)
    {
        this.aBuffers = aBuffers;
        this.bBuffers = bBuffers;
        this.currentAIndex = 0;
        this.currentBIndex = 0;
        this.currentASpan = currentASpan;
        this.currentBSpan = currentBSpan;
        this.offsetA = 0;
        this.offsetB = 0;
        this.CurrentA = ReadOnlySpan<T>.Empty;
        this.CurrentB = ReadOnlySpan<T>.Empty;
    }

    public ReadOnlySpan<T> CurrentA { get; private set; }

    public ReadOnlySpan<T> CurrentB { get; private set; }

    public ReadOnlySpan<ReadOnlyMemory<T>> RemainderA => this.aBuffers[currentAIndex..];

    public ReadOnlySpan<ReadOnlyMemory<T>> RemainderB => this.bBuffers[currentBIndex..];

    public bool MoveNext()
    {
        if (this.currentAIndex >= this.aBuffers.Length || this.currentBIndex >= this.bBuffers.Length)
        {
            return false;
        }

        int minLength = Math.Min(this.currentASpan.Length - this.offsetA, currentBSpan.Length - offsetB);
        this.CurrentA = this.currentASpan.Slice(this.offsetA, minLength);
        this.CurrentB = this.currentBSpan.Slice(this.offsetB, minLength);

        this.offsetA += minLength;
        this.offsetB += minLength;

        if (this.offsetA >= this.currentASpan.Length)
        {
            this.currentAIndex++;
            if (this.currentAIndex < this.aBuffers.Length)
            {
                this.currentASpan = this.aBuffers[currentAIndex].Span;
                this.offsetA = 0;
            }
        }

        if (this.offsetB >= this.currentBSpan.Length)
        {
            this.currentBIndex++;
            if (this.currentBIndex < this.bBuffers.Length)
            {
                this.currentBSpan = this.bBuffers[currentBIndex].Span;
                this.offsetB = 0;
            }
        }

        return true;
    }
}
