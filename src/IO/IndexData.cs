namespace Rope.IO;

public record class IndexData
{
	public int RowsPerRange { get; set; }
	public int BloomFilterSize { get; set; }
	public int HashFunctions { get; set; }
	public FileIndexData[] Files { get; set; }
}
