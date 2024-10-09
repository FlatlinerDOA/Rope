namespace Rope.IO;

public record class IndexMetaData
{
	public int RowsPerRange { get; set; }

	public int BloomFilterSize { get; set; }

	public int HashFunctions { get; set; }
}
