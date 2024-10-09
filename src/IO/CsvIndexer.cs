namespace Rope.IO;

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Rope;

public sealed class CsvIndexer : Indexer
{
	public CsvIndexer(IndexData indexData) : base(indexData)
	{
	}
	
	public CsvIndexer(int rowsPerRange = 1000, int bloomFilterSize = 1024, int hashFunctions = 3, SupportedOperationFlags supportedOperations = SupportedOperationFlags.StartsWith) : base(rowsPerRange, bloomFilterSize, hashFunctions, supportedOperations)
	{
    }

	public override string FileExtension => ".csv";


    public override ValueTask<FileIndex> IndexFileAsync(string filePath, DateTimeOffset lastWriteTimeUtc, Stream stream, CancellationToken cancellation = default)
	{
		var headers = Rope<string>.Empty;
		var meta = new FileMetaData(filePath, lastWriteTimeUtc, headers);

		int rowCount = 0;
		int rangeStartRow = 0;

		var fileRanges = new Dictionary<string, List<RowRange>>();
		Dictionary<string, BloomFilter> currentFilters = new();
		long startPosition = 0;
		long endPosition = 0;
		var cells = Rope<string>.Empty;
		using var reader = new StreamReader(stream);
		foreach (var line in StreamReaderExtensions.ReadCsvLines(reader))
		{
			cells = line.Cells.ToRope();
			if (headers.IsEmpty)
			{
				headers = cells;
				meta = meta with { Headers = headers };
				currentFilters = headers.ToDictionary(
					h => h,
					_ => new BloomFilter(BloomFilterSize, HashIterations, this.SupportedOperations)
				);

				fileRanges = headers.ToDictionary(h => h, h => new List<RowRange>());
				startPosition = line.EndOffset;				
			}
			else
			{
				for (int i = 0; i < Math.Min(headers.Count, cells.Count); i++)
				{
					currentFilters[headers[i]].Add(cells[i]);
				}
				
				rowCount++;

				endPosition = line.EndOffset;
				if (rowCount % RowsPerPage == 0)
				{
					for (int i = 0; i < headers.Count; i++)
					{
						fileRanges[headers[i]].Add(new RowRange
						{
							StartBytePosition = startPosition,
							EndBytePosition = endPosition,
							StartRowIndex = rangeStartRow,
							EndRowIndex = rowCount - 1,
							Filter = currentFilters[headers[i]]
						});
					}

					startPosition = endPosition;
					rangeStartRow = rowCount;
					currentFilters = headers.ToDictionary(
						h => h,
						_ => new BloomFilter(BloomFilterSize, HashIterations, this.SupportedOperations)
					);
				}
			}
		}

		// Handle the last range if it's not full
		if (rowCount % RowsPerPage != 0)
		{
			for (int i = 0; i < headers.Count; i++)
			{
				fileRanges[headers[i]].Add(new RowRange
				{
					StartBytePosition = startPosition,
					EndBytePosition = endPosition,
					StartRowIndex = rangeStartRow,
					EndRowIndex = rowCount - 1,
					Filter = currentFilters[headers[i]]
				});
			}
		}

		var fileIndex = new FileIndex(meta, fileRanges.ToDictionary(r => r.Key, r => r.Value.ToRope()));
		return ValueTask.FromResult(fileIndex);
		/*
		using (var reader = new StreamReader(filePath))
		{
			var headers = reader.ReadCsvLine(10);
			var fileRanges = new Dictionary<string, List<RowRange>>();

			foreach (var header in headers)
			{
				if (!fileRanges.ContainsKey(header))
					fileRanges[header] = new List<RowRange>();
			}

			long startPosition = reader.BaseStream.Position;
			int rowCount = 0;
			int rangeStartRow = 0;
			Dictionary<string, BloomFilter> currentFilters = headers.ToDictionary(
				h => h,
				_ => new BloomFilter(BloomFilterSize, HashFunctions)
			);

			while (!reader.EndOfStream)
			{
				var values = reader.ReadCsvLine(headers.Count);
				for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
				{
					currentFilters[headers[i]].Add(values[i]);
				}

				rowCount++;

				if (rowCount % RowsPerRange == 0)
				{
					long endPosition = reader.BaseStream.Position;
					for (int i = 0; i < headers.Count; i++)
					{
						fileRanges[headers[i]].Add(new RowRange
						{
							StartBytePosition = startPosition,
							EndBytePosition = endPosition,
							StartRowIndex = rangeStartRow,
							EndRowIndex = rowCount - 1,
							Filter = currentFilters[headers[i]]
						});
					}
					startPosition = endPosition;
					rangeStartRow = rowCount;
					currentFilters = headers.ToDictionary(
						h => h,
						_ => new BloomFilter(BloomFilterSize, HashFunctions)
					);
				}
			}

			// Handle the last range if it's not full
			if (rowCount % RowsPerRange != 0)
			{
				long endPosition = reader.BaseStream.Position;
				for (int i = 0; i < headers.Count; i++)
				{
					fileRanges[headers[i]].Add(new RowRange
					{
						StartBytePosition = startPosition,
						EndBytePosition = endPosition,
						StartRowIndex = rangeStartRow,
						EndRowIndex = rowCount - 1,
						Filter = currentFilters[headers[i]]
					});
				}
			}
			
			return new FileIndex(meta, fileRanges.ToDictionary(r => r.Key, r => r.Value.ToRope()));
		}
		*/
	}

	// public async IAsyncEnumerable<(FileMetaData File, IReadOnlyList<RowRange> Ranges)> ShortListRanges(Search search, [EnumeratorCancellation] CancellationToken cancellation)
	// {
	// 	// Step 1 - Short list ranges
	// 	foreach (var filePath in this.EnumerateFiles(null))
	// 	{
	// 		var index = await this.IndexFileAsync(filePath, cancellation);
	// 		if (search.ShouldSearch(index))
	// 		{
	// 			var ranges = new List<RowRange>();
	// 			foreach (var range in index.ColumnRanges[column])
	// 			{
	// 				if (range.Filter.MightContain(value))
	// 				{
	// 					ranges.Add(range);
	// 				}
	// 			}
				
	// 			if (ranges.Count > 0)
	// 			{
	// 				yield return (index.Meta, ranges);
	// 			}
	// 		}
	// 	}
	// }

	// public async IAsyncEnumerable<List<string>> SearchEquality(string column, string value, [EnumeratorCancellation] CancellationToken cancellation)
	// {
	// 	await foreach (var (file, ranges) in this.ShortListRanges(column, value, cancellation))
	// 	{
	// 		foreach (var result in SearchInRange(file, column, value, ranges, (a, b) => a == b))
	// 		{
	// 			yield return result;
	// 		}
	// 	}
	// }

	public async IAsyncEnumerable<Dictionary<string, string>> Search(string folderPath, Search search, [EnumeratorCancellation] CancellationToken cancellation)
	{
		// Step 1 - Short list ranges
		foreach (var filePath in this.EnumerateFiles(folderPath))
		{
			// Step 2 - Should this search even bother opening this file?
			var index = await this.IndexFileAsync(filePath, cancellation);
			if (search.ShouldSearch(index)) 
			{
				var headers = index.Meta.Headers;
				var (stream, lastWrite) = this.ReadFile(filePath);
				using (stream)
				{
					// Step 3 - Get row ranges that are relevant to this search criteria.
					// TODO: Use SortedSet?
					foreach (var (startOffset, endOffset) in search.SearchablePages(index)) 
					{
						var bufferSize = (int)(endOffset - startOffset);
						var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
						var mem = buffer.AsMemory()[..bufferSize];
                        stream.Seek(startOffset, SeekOrigin.Begin);
                        await stream.ReadExactlyAsync(mem, cancellation);						
						using var reader = new StreamReader(new ReadOnlyMemoryStream(mem));
						var values = reader.ReadCsvLine(headers.Count, out var bytesConsumed).ToRope();
						if (search.Matches(values, headers))
						{
							yield return new Dictionary<string, string>(headers.Zip(values, (k,v) => new KeyValuePair<string, string>(k, v)));
						}
                        
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
				}
			}
		
		}
	}

	// public async IAsyncEnumerable<List<string>> SearchStartsWith(string column, string prefix, [EnumeratorCancellation] CancellationToken cancellation)
	// {
	// 	await foreach (var (file, ranges) in this.ShortListRanges(column, prefix, cancellation))
	// 	{
	// 		foreach (var result in SearchInRange(file, column, prefix, ranges, (a, b) => a.StartsWith(b)))
	// 		{
	// 			yield return result;
	// 		}
	// 	}
	// }

	// private IEnumerable<List<string>> SearchInRange(FileMetaData file, string column, string value, IEnumerable<RowRange> ranges, Func<string, string, bool> matches)
	// {
	// 	var headers = file.Headers;
	// 	var columnIndex = (int)headers.IndexOf(column);
    //     var (reader, lastWrite) = this.ReadFile(file.Path);
    //     using (reader)
	// 	{
	// 		foreach (var range in ranges.OrderBy(r => r.StartBytePosition))
	// 		{
	// 			reader.Seek(range.StartBytePosition);
	// 			while (reader.Peek() != -1)
	// 			{
	// 				var values = reader.ReadCsvLine(headers.Count, out var bytesConsumed);
	// 				if (matches(values[columnIndex], value))
	// 				{
	// 					yield return values;
	// 				}
	// 			}
	// 		}
	// 	}
	// }

	// private IEnumerable<Dictionary<string, string>> SearchInRange(FileMetaData file, IEnumerable<RowRange> ranges, Func<Rope<string>, bool> matches)
	// {
	// 	var headers = file.Headers;
    //     var (reader, lastWriteTime) = this.ReadFile(file.Path);
    //     using (reader)
	// 	{
	// 		foreach (var range in ranges.OrderBy(r => r.StartBytePosition))
	// 		{
	// 			reader.Seek(range.StartBytePosition);
	// 			while (reader.Peek() != -1)
	// 			{
	// 				var values = reader.ReadCsvLine(headers.Count, out var bytesConsumed).ToRope();
	// 				if (matches(values))
	// 				{
	// 					yield return headers.Zip(values).ToDictionary(h => h.First, h => h.Second);
	// 				}
	// 			}
	// 		}
	// 	}
	// }

	public async Task SaveIndexToJson(string filePath)
    {
        var options = new JsonSerializerOptions
        {
			WriteIndented = true,
			Converters =
            {
                new RowRangeJsonConverter()
                {
                    BloomFilterSize = this.BloomFilterSize,
                    HashIterations = this.HashIterations,
                    SupportedOperations = this.SupportedOperations
                }
            }
		};

		var fileIndexes = await Task.WhenAll(this.Files.Values.Select(v => v.AsTask()));
		var indexData = new IndexData()
		{
			RowsPerPage = this.RowsPerPage,
			BloomFilterSize = this.BloomFilterSize,
			HashIterations = this.HashIterations,
            SupportedOperations = this.SupportedOperations,
			Files = fileIndexes.OrderBy(f => f.Meta.Path).Select(v => new FileIndexData(
				v.Meta.Path, 
				v.Meta.LastModifiedUtc, 
				v.ColumnRanges.Select(cr => new ColumnIndexData(cr.Key, cr.Value)).ToArray())).ToArray()
		};

		using var f = File.OpenWrite(filePath);
		await JsonSerializer.SerializeAsync(f, indexData, options);		
	}

	public static CsvIndexer LoadIndexFromJson(string filePath)
	{
		string jsonString = File.ReadAllText(filePath);

		// Load just the index meta data so that we can deserialize the bloom filters 
		// without repeating their configuration in the JSON.
		var meta = JsonSerializer.Deserialize<IndexMetaData>(jsonString);

        if (meta != null)
        {
            var options = new JsonSerializerOptions
            {
                Converters =
            {
                new RowRangeJsonConverter()
                {
                    BloomFilterSize = meta.BloomFilterSize,
                    HashIterations = meta.HashIterations,
                    SupportedOperations = meta.SupportedOperations,
                },
                new RopeJsonConverter<RowRange>(),
                new RopeJsonConverter<string>()
            }
            };

            var indexData = JsonSerializer.Deserialize<IndexData>(jsonString, options);

            var indexer = new CsvIndexer(indexData);

            return indexer;
        }

        return null;
	}
}
