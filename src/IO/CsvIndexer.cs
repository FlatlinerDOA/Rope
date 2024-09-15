namespace Rope.IO;

using System.Buffers;
using System.Text.Json;
using Rope;

public sealed class CsvIndexer : Indexer
{
	public CsvIndexer(IndexData indexData) : base(indexData)
	{
	}
	
	public CsvIndexer(int rowsPerRange = 1000, int bloomFilterSize = 1024, int hashFunctions = 3): base(rowsPerRange, bloomFilterSize, hashFunctions)
	{
	}
	
	public override string FileExtension => ".csv";

	public override async ValueTask<FileIndex> IndexFile(string filePath, DateTimeOffset lastWriteTimeUtc, TextReader reader, CancellationToken cancellation = default)
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
		foreach (var line in StreamReaderExtensions.ReadCsvLines(reader))
		{
			cells = line.Cells.ToRope();
			if (headers.IsEmpty)
			{
				headers = cells;
				meta = meta with { Headers = headers };
				currentFilters = headers.ToDictionary(
					h => h,
					_ => new BloomFilter(BloomFilterSize, HashFunctions, SupportedOperationFlags.StartsWith)
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
				if (rowCount % RowsPerRange == 0)
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
						_ => new BloomFilter(BloomFilterSize, HashFunctions, SupportedOperationFlags.StartsWith)
					);
				}
			}
		}

		// Handle the last range if it's not full
		if (rowCount % RowsPerRange != 0)
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
		return fileIndex;
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

	public async IAsyncEnumerable<(FileMetaData File, IReadOnlyList<RowRange> Ranges)> ShortListRanges(string column, string value)
	{
		// Step 1 - Short list ranges
		foreach (var (filePath, indexTask) in this.Files.ToList())
		{
			var index = await indexTask;
			if (index.ColumnRanges.ContainsKey(column))
			{
				var ranges = new List<RowRange>();
				foreach (var range in index.ColumnRanges[column])
				{
					if (range.Filter.MightContain(value))
					{
						ranges.Add(range);
					}
				}
				
				yield return (index.Meta, ranges);
			}
		}
	}

	public async IAsyncEnumerable<List<string>> SearchEquality(string column, string value)
	{
		await foreach (var (file, ranges) in this.ShortListRanges(column, value))
		{
			foreach (var result in SearchInRange(file, column, value, ranges, (a, b) => a == b))
			{
				yield return result;
			}
		}
	}

	public async IAsyncEnumerable<Dictionary<string, string>> Search(params Search[] all)
	{
		foreach (var search in all.Take(1))
		{
			await foreach (var (file, ranges) in this.ShortListRanges(search.Column, search.Value))
			{
				var headers = file.Headers;
				if (all.All(s => headers.Contains(s.Column)))
				{
					foreach (var result in SearchInRange(file, ranges, (values) => all.All(s => s.Matches(values, headers))))
					{
						yield return result;
					}
				}			
			}
		}
	}
		

	public async IAsyncEnumerable<List<string>> SearchStartsWith(string column, string prefix)
	{
		await foreach (var (file, ranges) in this.ShortListRanges(column, prefix))
		{
			foreach (var result in SearchInRange(file, column, prefix, ranges, (a, b) => a.StartsWith(b)))
			{
				yield return result;
			}
		}
	}

	private IEnumerable<List<string>> SearchInRange(FileMetaData file, string column, string value, IEnumerable<RowRange> ranges, Func<string, string, bool> matches)
	{
		var headers = file.Headers;
		var columnIndex = (int)headers.IndexOf(column);
		using (var reader = new StreamReader(file.Path))
		{
			foreach (var range in ranges.OrderBy(r => r.StartBytePosition))
			{
				reader.BaseStream.Seek(range.StartBytePosition, SeekOrigin.Begin);
				while (reader.BaseStream.Position < range.EndBytePosition && !reader.EndOfStream)
				{
					var values = reader.ReadCsvLine(headers.Count, out var bytesConsumed);
					if (matches(values[columnIndex], value))
					{
						yield return values;
					}
				}
			}
		}
	}

	private IEnumerable<Dictionary<string, string>> SearchInRange(FileMetaData file, IEnumerable<RowRange> ranges, Func<Rope<string>, bool> matches)
	{
		var headers = file.Headers;
		using (var reader = new StreamReader(file.Path))
		{
			foreach (var range in ranges.OrderBy(r => r.StartBytePosition))
			{
				reader.BaseStream.Seek(range.StartBytePosition, SeekOrigin.Begin);
				while (reader.BaseStream.Position < range.EndBytePosition && !reader.EndOfStream)
				{
					var values = reader.ReadCsvLine(headers.Count, out var bytesConsumed).ToRope();
					if (matches(values))
					{
						yield return headers.Zip(values).ToDictionary(h => h.First, h => h.Second);
					}
				}
			}
		}
	}

	public async ValueTask SaveIndexToJson(string filePath)
    {
        var options = new JsonSerializerOptions
        {
			WriteIndented = true,
			Converters = { new RowRangeJsonConverter() }
		};

		var fileIndexes = await Task.WhenAll<FileIndex>(this.Files.Values.Select(v => v.AsTask()).ToList());
		var indexData = new IndexData()
		{
			RowsPerRange = this.RowsPerRange,
			BloomFilterSize = BloomFilterSize,
			HashFunctions = HashFunctions,
			Files = fileIndexes.Select(v => new FileIndexData(
				v.Meta.Path, 
				v.Meta.LastModifiedUtc, 
				v.ColumnRanges.Select(cr => new ColumnIndexData(cr.Key, cr.Value.ToRope())).ToArray())).ToArray()
		};

		using var f = File.OpenWrite(filePath);
		await JsonSerializer.SerializeAsync(f, options);		
	}

	public static CsvIndexer LoadIndexFromJson(string filePath)
	{
		string jsonString = File.ReadAllText(filePath);

		// Load just the index meta data so that we can deserialize the bloom filters 
		// without repeating their configuration in the JSON.
		var meta = JsonSerializer.Deserialize<IndexMetaData>(jsonString);

		var options = new JsonSerializerOptions
		{
			Converters =
			{ 
				new RowRangeJsonConverter()
				{
					BloomFilterSize = meta.BloomFilterSize,
					HashFunctions = meta.HashFunctions,
				},
				new RopeJsonConverter<RowRange>(),
				new RopeJsonConverter<string>()
			}
		};

		var indexData = JsonSerializer.Deserialize<IndexData>(jsonString, options);

		var indexer = new CsvIndexer(indexData);

		return indexer;
	}
}
