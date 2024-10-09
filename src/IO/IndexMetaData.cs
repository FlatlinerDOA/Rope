namespace Rope.IO;

public record class IndexMetaData
{
    public int RowsPerPage { get; init; }

    public int BloomFilterSize { get; init; }

    public int HashIterations { get; init; }

    public SupportedOperationFlags SupportedOperations { get; init; }
}
