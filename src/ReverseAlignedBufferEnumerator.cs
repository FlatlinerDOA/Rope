namespace Rope;

public ref struct ReverseAlignedBufferEnumerator<T>
{
    private ReadOnlySpan<ReadOnlyMemory<T>> aBuffers;
    private ReadOnlySpan<ReadOnlyMemory<T>> bBuffers;

    private int currentAIndex;
    private int currentBIndex;

    private ReadOnlySpan<T> currentASpan;
    private ReadOnlySpan<T> currentBSpan;

    private int offsetA;
    private int offsetB;

    public ReverseAlignedBufferEnumerator(ReadOnlySpan<ReadOnlyMemory<T>> aBuffers, ReadOnlySpan<ReadOnlyMemory<T>> bBuffers) : this(aBuffers[^1].Span, bBuffers[^1].Span, aBuffers, bBuffers)
    {
    }

    public ReverseAlignedBufferEnumerator(ReadOnlySpan<T> currentASpan, ReadOnlySpan<T> currentBSpan, ReadOnlySpan<ReadOnlyMemory<T>> aBuffers, ReadOnlySpan<ReadOnlyMemory<T>> bBuffers)
    {
        this.aBuffers = aBuffers;
        this.bBuffers = bBuffers;
        this.currentAIndex = aBuffers.Length - 1;
        this.currentBIndex = bBuffers.Length - 1;
        this.currentASpan = currentASpan;
        this.currentBSpan = currentBSpan;
        this.offsetA = this.currentASpan.Length;
        this.offsetB = this.currentBSpan.Length;
        this.CurrentA = ReadOnlySpan<T>.Empty;
        this.CurrentB = ReadOnlySpan<T>.Empty;
    }

    public ReadOnlySpan<T> CurrentA { get; private set; }
    public ReadOnlySpan<T> CurrentB { get; private set; }

    public bool HasRemainderA => this.currentAIndex >= 0;

    public bool HasRemainderB => this.currentBIndex >= 0;

    public bool MoveNext()
    {
        if (this.currentAIndex < 0 || this.currentBIndex < 0)
        {
            return false;
        }

        int minLength = Math.Min(this.offsetA, this.offsetB);
        this.CurrentA = this.currentASpan.Slice(this.offsetA - minLength, minLength);
        this.CurrentB = this.currentBSpan.Slice(this.offsetB - minLength, minLength);

        this.offsetA -= minLength;
        this.offsetB -= minLength;

        if (this.offsetA == 0)
        {
            this.currentAIndex--;
            if (this.currentAIndex >= 0)
            {
                this.currentASpan = this.aBuffers[this.currentAIndex].Span;
                this.offsetA = this.currentASpan.Length;
            }
        }

        if (this.offsetB == 0)
        {
            this.currentBIndex--;
            if (this.currentBIndex >= 0)
            {
                this.currentBSpan = this.bBuffers[this.currentBIndex].Span;
                this.offsetB = this.currentBSpan.Length;
            }
        }

        return true;
    }
}