namespace Rope.IO;

using System.Collections.Concurrent;
using System.Diagnostics;
using Rope;

public abstract class Indexer
{
	public Indexer(IndexData indexData)
	{
		this.RowsPerRange = indexData.RowsPerRange;
		this.BloomFilterSize = indexData.BloomFilterSize;
		this.HashFunctions = indexData.HashFunctions;
		this.Files = new(indexData.Files.ToDictionary(
			c => c.FilePath,
			c => ValueTask.FromResult(new FileIndex(new FileMetaData(c.FilePath, c.LastModifiedUtc, c.Headers), c.Columns.ToDictionary(f => f.Name, f => f.Ranges.ToRope())))));
	}

	public Indexer(int rowsPerRange = 1000, int bloomFilterSize = 1024, int hashFunctions = 3)
	{
		this.RowsPerRange = rowsPerRange;
		this.BloomFilterSize = bloomFilterSize;
		this.HashFunctions = hashFunctions;
		this.Files = new();
	}

	public int RowsPerRange { get; init; }
	public int BloomFilterSize { get; init; }
	public int HashFunctions { get; init; }
	public abstract string FileExtension { get; }
	protected ConcurrentDictionary<string, ValueTask<FileIndex>> Files { get; }

	public async Task IndexAllFilesInFolder(string folderPath)
	{
		var options = new EnumerationOptions()
		{
			RecurseSubdirectories = true,
			IgnoreInaccessible = true,
			MatchCasing = MatchCasing.CaseInsensitive
		};

		var filePaths = Directory.EnumerateFiles(folderPath, $"*{this.FileExtension}", options).OrderBy(d => d, StringComparer.OrdinalIgnoreCase);
		await Parallel.ForEachAsync(
			filePaths,
			async (filePath, c) =>
			{
				var s = Stopwatch.StartNew();
				var i = await this.IndexFile(filePath, c);
				Trace.WriteLine($"Index {filePath} took {s.ElapsedMilliseconds}ms");
			});
	}

	public virtual ValueTask<FileIndex> IndexFile(string filePath, CancellationToken cancellation = default)
	{
		return this.Files.AddOrUpdate(
			filePath,
			async (filePath) =>
			{
				var lastWriteTimeUtc = new FileInfo(filePath).LastWriteTimeUtc;
				using var sr = new StreamReader(filePath);
				return await this.IndexFile(filePath, lastWriteTimeUtc, sr, cancellation);
			},
			async (filePath, existing) =>
			{
				var lastWriteTimeUtc = new FileInfo(filePath).LastWriteTimeUtc;
				var e = await existing;
				if (e.Meta.LastModifiedUtc >= lastWriteTimeUtc)
				{
					return e;
				}

				using var sr = new StreamReader(filePath);
				return await this.IndexFile(filePath, lastWriteTimeUtc, sr, cancellation);
			});
	}

	public abstract ValueTask<FileIndex> IndexFile(string filePath, DateTimeOffset lastWriteTimeUtc, TextReader reader, CancellationToken cancellation);	
}
