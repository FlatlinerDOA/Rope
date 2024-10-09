namespace Rope.IO;

public record class RowRange
{
	public required long StartBytePosition { get; init; }
	public required long EndBytePosition { get; init; }
	public required int StartRowIndex { get; init; }
	public required int EndRowIndex { get; init; }
	public required BloomFilter Filter { get; init; }
}
