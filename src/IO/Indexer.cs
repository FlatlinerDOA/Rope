namespace Rope.IO;

using System.Collections.Concurrent;
using System.Diagnostics;
using Rope;

public abstract class Indexer
{
	public Indexer(IndexData indexData)
	{
        this.EnumerateFiles = (folderPath) => EnumerateLocalFiles(folderPath, this.FileExtension);
        this.RowsPerPage = indexData.RowsPerPage;
		this.BloomFilterSize = indexData.BloomFilterSize;
		this.HashIterations = indexData.HashIterations;
        this.LastCommitRef = indexData.LastCommitRef;
        this.SupportedOperations = indexData.SupportedOperations;
        this.Files = new(indexData.Files.ToDictionary(
			c => c.FilePath,
			c => ValueTask.FromResult(new FileIndex(new FileMetaData(c.FilePath, c.LastModifiedUtc, c.Headers), c.Columns.ToDictionary(f => f.Name, f => f.Ranges.ToRope())))));
	}

	public Indexer(int rowsPerPage = 1000, int bloomFilterSize = 1024, int hashIterations = 3, SupportedOperationFlags supportedOperations = SupportedOperationFlags.StartsWith)
	{
        this.EnumerateFiles = (folderPath) => EnumerateLocalFiles(folderPath, this.FileExtension);
        this.RowsPerPage = rowsPerPage;
		this.BloomFilterSize = bloomFilterSize;
		this.HashIterations = hashIterations;
        this.SupportedOperations = supportedOperations;
        this.Files = new();
	}

    public Func<string, IEnumerable<string>> EnumerateFiles { get; init; }

    public Func<string, (Stream stream, DateTime LastWriteUtc)> ReadFile { get; init; } = OpenLocalFile;

    public SupportedOperationFlags SupportedOperations { get; init; }

    public int RowsPerPage { get; init; }

	public int BloomFilterSize { get; init; }

	public int HashIterations { get; init; }

	public abstract string FileExtension { get; }

    public string? LastCommitRef { get; init; }

    protected ConcurrentDictionary<string, ValueTask<FileIndex>> Files { get; }

    private static readonly EnumerationOptions FileEnumerationOptions = new EnumerationOptions()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive
    };

    public static IEnumerable<string> EnumerateLocalFiles(string folderPath, string fileExtension) => Directory.Exists(folderPath) ? Directory.EnumerateFiles(folderPath, $"*{fileExtension}", FileEnumerationOptions)
        .OrderBy(d => d, StringComparer.OrdinalIgnoreCase) : Array.Empty<string>();

    public static (Stream stream, DateTime LastWriteUtc) OpenLocalFile(string filePath) => (File.OpenRead(filePath), new FileInfo(filePath).LastWriteTimeUtc);

    public async Task IndexAllFilesInFolderAsync(string folderPath, CancellationToken cancellation)
	{
        await IndexFilesAsync(this.EnumerateFiles(folderPath), cancellation);
    }

    public async Task IndexFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellation)
    {
        await Parallel.ForEachAsync(
            filePaths.Where(p => Path.GetExtension(p) == this.FileExtension),
            async (filePath, c) =>
            {
                var s = Stopwatch.StartNew();
                var i = await this.IndexFileAsync(filePath, c);
                Trace.WriteLine($"Index {filePath} took {s.ElapsedMilliseconds}ms");
            });
    }

    public virtual ValueTask<FileIndex> IndexFileAsync(string filePath, CancellationToken cancellation = default)
	{
		return this.Files.AddOrUpdate(
			filePath,
			async (filePath) =>
			{
				var (stream, lastWriteTimeUtc) = this.ReadFile(filePath);
                using (stream)
                {
                    return await this.IndexFileAsync(filePath, lastWriteTimeUtc, stream, cancellation);
                }
			},
			async (filePath, existing) =>
			{
				var e = await existing;
                var (stream, lastWriteTimeUtc) = this.ReadFile(filePath);
                using (stream)
                {
                    if (e.Meta.LastModifiedUtc >= lastWriteTimeUtc)
                    {
                        return e;
                    }

                    return await this.IndexFileAsync(filePath, lastWriteTimeUtc, stream, cancellation);
                }
			});
	}

	public abstract ValueTask<FileIndex> IndexFileAsync(string filePath, DateTimeOffset lastWriteTimeUtc, Stream stream, CancellationToken cancellation);

    public virtual bool RemoveFile(string filePath, CancellationToken cancellation) => this.Files.Remove(filePath, out _);
}
