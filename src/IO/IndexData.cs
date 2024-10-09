namespace Rope.IO;

public record class IndexData
{
	public int RowsPerPage { get; init; }
	
    public int BloomFilterSize { get; init; }
	
    public int HashIterations { get; init; }

    public SupportedOperationFlags SupportedOperations { get; init; }

    public string? LastCommitRef { get; init; }

    public required FileIndexData[] Files { get; init; }
}
