namespace Rope.IO;

public record class RowRange
{
	public long StartBytePosition { get; set; }
	public long EndBytePosition { get; set; }
	public int StartRowIndex { get; set; }
	public int EndRowIndex { get; set; }
	public BloomFilter Filter { get; set; }
}
