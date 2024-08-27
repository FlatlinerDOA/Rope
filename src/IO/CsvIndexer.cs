namespace Rope.IO;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Experimental CSV Indexing system.
/// </summary>
public sealed class CsvIndexer
{
	private readonly Dictionary<string, Dictionary<string, List<RowRange>>> fileColumnRanges;
	private readonly Dictionary<string, FileMetaData> files = new();

	public int RowsPerRange { get; }
	public int BloomFilterSize { get; }
	public int HashFunctions { get; }

	public CsvIndexer(IndexData indexData)
	{
		this.RowsPerRange = indexData.RowsPerRange;
		this.BloomFilterSize = indexData.BloomFilterSize;
		this.HashFunctions = indexData.HashFunctions;
		this.files = indexData.Files.ToDictionary(c => c.FilePath, c => new FileMetaData(c.FilePath, c.LastModifiedUtc, c.Headers));
		this.fileColumnRanges = indexData.Files.ToDictionary(c => c.FilePath, c => c.Columns.ToDictionary(f => f.Name, f => f.Ranges));
	}

	public CsvIndexer(int rowsPerRange = 1000, int bloomFilterSize = 1024, int hashFunctions = 3)
	{
		this.RowsPerRange = rowsPerRange;
		this.BloomFilterSize = bloomFilterSize;
		this.HashFunctions = hashFunctions;
		this.fileColumnRanges = new Dictionary<string, Dictionary<string, List<RowRange>>>();
	}

	public void IndexCsvFiles(IReadOnlyList<string> filePaths)
	{
		foreach (string filePath in filePaths)
		{
			this.IndexFile(filePath);			
		}
	}
	
	public void IndexFile(string filePath)
	{
		using (var reader = new StreamReader(filePath))
		{
			var headers = reader.ReadCsvLine(10);
			this.files[filePath] = new FileMetaData(filePath, new FileInfo(filePath).LastWriteTimeUtc, headers);
			var fileRanges = new Dictionary<string, List<RowRange>>();
			this.fileColumnRanges[filePath] = fileRanges;

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
						fileColumnRanges[filePath][headers[i]].Add(new RowRange
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
					fileColumnRanges[filePath][headers[i]].Add(new RowRange
					{
						StartBytePosition = startPosition,
						EndBytePosition = endPosition,
						StartRowIndex = rangeStartRow,
						EndRowIndex = rowCount - 1,
						Filter = currentFilters[headers[i]]
					});
				}
			}
		}
	}

	public IEnumerable<(string FilePath, IReadOnlyList<RowRange> Ranges)> ShortListRanges(string column, string value)
	{
		// Step 1 - Short list ranges
		foreach (var filePath in fileColumnRanges.Keys)
		{
			if (fileColumnRanges[filePath].ContainsKey(column))
			{
				var ranges = new List<RowRange>();
				foreach (var range in fileColumnRanges[filePath][column])
				{
					if (range.Filter.MightContain(value))
					{
						ranges.Add(range);
					}
				}
				
				yield return (filePath, ranges);
			}
		}
	}

	public IEnumerable<List<string>> SearchEquality(string column, string value)
	{
		var fileRanges = this.ShortListRanges(column, value);
		foreach (var ranges in fileRanges)
		{
			foreach (var result in SearchInRange(ranges.FilePath, column, value, ranges.Ranges, (a, b) => a == b))
			{
				yield return result;
			}
		}
	}

	public IEnumerable<Dictionary<string, string>> Search(params Search[] all)
	{
		return from search in all.Take(1)
			from ranges in this.ShortListRanges(search.Column, search.Value)
			let headers = this.files[ranges.FilePath].Headers
			where all.All(s => headers.Contains(s.Column))
			from result in SearchInRange(ranges.FilePath, ranges.Ranges, (values) => all.All(s => s.Matches(values, headers)))
			select result;
	}
		

	public IEnumerable<List<string>> SearchStartsWith(string column, string prefix)
	{
		var fileRanges = this.ShortListRanges(column, prefix);
		foreach (var ranges in fileRanges)
		{
			foreach (var result in SearchInRange(ranges.FilePath, column, prefix, ranges.Ranges, (a, b) => a.StartsWith(b)))
			{
				yield return result;
			}
		}
	}

	private IEnumerable<List<string>> SearchInRange(string filePath, string column, string value, IEnumerable<RowRange> ranges, Func<string, string, bool> matches)
	{
		var headers = files[filePath].Headers;
		int columnIndex = headers.IndexOf(column);
		using (var reader = new StreamReader(filePath))
		{
			foreach (var range in ranges.OrderBy(r => r.StartBytePosition))
			{
				reader.BaseStream.Seek(range.StartBytePosition, SeekOrigin.Begin);
				while (reader.BaseStream.Position < range.EndBytePosition && !reader.EndOfStream)
				{
					var values = reader.ReadCsvLine(headers.Count);
					if (matches(values[columnIndex], value))
					{
						yield return values;
					}
				}
			}
		}
	}

	private IEnumerable<Dictionary<string, string>> SearchInRange(string filePath, IEnumerable<RowRange> ranges, Func<List<string>, bool> matches)
	{
		var headers = files[filePath].Headers;
		using (var reader = new StreamReader(filePath))
		{
			foreach (var range in ranges.OrderBy(r => r.StartBytePosition))
			{
				reader.BaseStream.Seek(range.StartBytePosition, SeekOrigin.Begin);
				while (reader.BaseStream.Position < range.EndBytePosition && !reader.EndOfStream)
				{
					var values = reader.ReadCsvLine(headers.Count);
					if (matches(values))
					{
						yield return headers.Zip(values).ToDictionary(h => h.First, h => h.Second);
					}
				}
			}
		}
	}

	public void SaveIndexToJson(string filePath)
    {
        var options = new JsonSerializerOptions
        {
			WriteIndented = true,
			Converters = { new RowRangeJsonConverter() }
		};

		var indexData = new IndexData()
		{
			RowsPerRange = this.RowsPerRange,
			BloomFilterSize = BloomFilterSize,
			HashFunctions = HashFunctions,
			Files = this.files.Values.Select(v => new FileIndexData(
				v.FilePath, 
				v.LastModifiedUtc, 
				fileColumnRanges[v.FilePath].Select(cr => new ColumnIndexData(cr.Key, cr.Value)).ToArray())).ToArray()
		};

		string jsonString = JsonSerializer.Serialize(indexData, options);
		File.WriteAllText(filePath, jsonString);
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
				}
			}
		};

		var indexData = JsonSerializer.Deserialize<IndexData>(jsonString, options);

		var indexer = new CsvIndexer(indexData);

		return indexer;
	}
}


public record class Search(string Column, string Value, bool StartsWith = false)
{
	public bool Matches(List<string> values, List<string> headers)
	{
		var c = headers.IndexOf(this.Column);
		return c >= 0 && c < values.Count && this.Matches(values[c]);
	}
	
	public bool Matches(string value) => StartsWith ? value.StartsWith(this.Value) : value == this.Value;
}

public class BloomFilter
{
	private bool[] bits;
	public int HashFunctions { get; init; }
	public int Size { get; init; }

	public BloomFilter(int size, int hashFunctions, string runLengthEncodedBits)
	{
		this.Size = size;
		this.HashFunctions = hashFunctions;
		this.SerializedBits = runLengthEncodedBits;
	}
	
	public BloomFilter(int size, int hashFunctions, bool[] bits = null)
	{
		this.Size = size;
		this.HashFunctions = hashFunctions;
	   	this.bits = bits ?? new bool[size];
	}

	public bool[] Bits => bits;

	public string SerializedBits
    {
        get => Convert.ToBase64String(RunLengthEncode(bits));
        set
        {
            byte[] rleData = Convert.FromBase64String(value);
            bits = RunLengthDecode(rleData, Size);
        }
    }

    private static byte[] RunLengthEncode(bool[] data)
    {
        var result = new List<byte>();
        int count = 1;
        bool current = data[0];

		for (int i = 1; i < data.Length; i++)
		{
			if (data[i] == current && count < 255)
			{
				count++;
			}
			else
			{
				result.Add((byte)(current ? count | 0x80 : count));
				current = data[i];
				count = 1;
			}
		}
		result.Add((byte)(current ? count | 0x80 : count));

		return result.ToArray();
	}

	private static bool[] RunLengthDecode(byte[] rleData, int originalLength)
	{
		var result = new bool[originalLength];
		int index = 0;

		foreach (byte b in rleData)
		{
			bool value = (b & 0x80) != 0;
			int count = b & 0x7F;

			for (int i = 0; i < count && index < originalLength; i++)
			{
				result[index++] = value;
			}
		}

		return result;
	}

	public void Add(string item)
    {
        for (int i = 0; i < Math.Min(item.Length, this.Size); i++)
        {
            AddCharAtIndex(item[i], i);
        }
    }

    private void AddCharAtIndex(char c, int index)
	{
		var primaryHash = HashInt32((uint)c);
		var secondaryHash = HashInt32((uint)index);
		for (int i = 0; i < HashFunctions; i++)
		{
			int hash = ComputeHash(c, index, i);
			bits[hash] = true;
		}
	}

	public bool MightContain(string prefix)
	{
		for (int i = 0; i < Math.Min(prefix.Length, this.Size); i++)
		{
			if (!MightContainCharAtIndex(prefix[i], i))
				return false;
		}
		
		return true;
	}

	private bool MightContainCharAtIndex(char c, int index)
	{
		var primaryHash = HashInt32((uint)c);
		var secondaryHash = HashInt32((uint)index);
		for (int i = 0; i < this.HashFunctions; i++)
		{
			int hash = ComputeHash(c, index, i);
			if (!bits[hash])
			{
				return false;
			}
		}
		
		return true;
	}

	/// <summary>
	/// Performs Dillinger and Manolios double hashing. 
	/// </summary>
	/// <param name="primaryHash"> The primary hash. </param>
	/// <param name="secondaryHash"> The secondary hash. </param>
	/// <param name="i"> The i. </param>
	/// <returns> The <see cref="int"/>. </returns>
	private int ComputeHash(int primaryHash, int secondaryHash, int i)
	{
		int resultingHash = (primaryHash + (i * secondaryHash)) % this.Size;
		return Math.Abs((int)resultingHash);
	}

	/// <summary>
	/// Hashes a 32-bit signed int using Thomas Wang's method v3.1 original link (http://www.concentric.net/~Ttwang/tech/inthash.htm).
	/// Runtime is suggested to be 11 cycles. 
	/// Analysis - https://burtleburtle.net/bob/hash/integer.html
	/// </summary>
	/// <param name="input">The integer to hash.</param>
	/// <returns>The hashed result.</returns>
	private static int HashInt32(uint x)
	{
		unchecked
		{
			x = ~x + (x << 15); // x = (x << 15) - x- 1, as (~x) + y is equivalent to y - x - 1 in two's complement representation
			x = x ^ (x >> 12);
			x = x + (x << 2);
			x = x ^ (x >> 4);
			x = x * 2057; // x = (x + (x << 3)) + (x<< 11);
			x = x ^ (x >> 16);
			return (int)x;
		}
	}
}

public class RowRange
{
	public long StartBytePosition { get; set; }
	public long EndBytePosition { get; set; }
	public int StartRowIndex { get; set; }
	public int EndRowIndex { get; set; }
	public BloomFilter Filter { get; set; }
}

public static class StreamReaderExtensions
{
	public static List<string> ReadCsvLine(this TextReader reader, int capacity)
	{
		bool isInQuote = false;
		var b = new StringBuilder();
		var list = new List<string>(capacity);
		while (reader.Peek() >= 0)
        {
            var c = (char)reader.Read();
			switch (c) 
			{
				case '\n':
				case '\r':
					if (isInQuote)
					{
						b.Append(c);
					}
					else
					{
						list.Add(b.ToString());
						return list;
					}
					
					break;

				case ',':
					if (isInQuote)
					{
						b.Append(c);
					}
					else
					{
						list.Add(b.ToString());
						b = new StringBuilder();
					}
					
					break;
					
				case '\"':
					isInQuote = !isInQuote;
					break;
					
				default:
					b.Append(c);
					break;
			}
		}

		list.Add(b.ToString());
		return list;
	}
}


public record class IndexMetaData
{
	public int RowsPerRange { get; set; }
	public int BloomFilterSize { get; set; }
	public int HashFunctions { get; set; }
}
public record class IndexData
{
	public int RowsPerRange { get; set; }
	public int BloomFilterSize { get; set; }
	public int HashFunctions { get; set; }
	public FileIndexData[] Files { get; set; }
}

public record class FileMetaData(string FilePath, DateTimeOffset LastModifiedUtc, List<string> Headers);
public record class FileIndexData(string FilePath, DateTimeOffset LastModifiedUtc, ColumnIndexData[] Columns)
{
	[JsonIgnore]
	public List<string> Headers => this.Columns.Select(c => c.Name).ToList();
};

public record class ColumnIndexData(string Name, List<RowRange> Ranges);


public class RowRangeJsonConverter : JsonConverter<RowRange>
{
	public int BloomFilterSize  { get; init; }
	public int HashFunctions { get; init; }
	
	public override RowRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var rowRange = new RowRange();
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return rowRange;
			}

			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string propertyName = reader.GetString();
				reader.Read();
				switch (propertyName)
				{
					case "s":
						rowRange.StartBytePosition = reader.GetInt64();
						break;
					case "e":
						rowRange.EndBytePosition = reader.GetInt64();
						break;
					case "sr":
						rowRange.StartRowIndex = reader.GetInt32();
						break;
					case "er":
						rowRange.EndRowIndex = reader.GetInt32();
						break;
					case "f":
						rowRange.Filter = new BloomFilter(this.BloomFilterSize, this.HashFunctions, reader.GetString());
						break;
				}
			}
		}
		
		throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, RowRange value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("s", value.StartBytePosition);
		writer.WriteNumber("e", value.EndBytePosition);
		writer.WriteNumber("sr", value.StartRowIndex);
		writer.WriteNumber("er", value.EndRowIndex);
		writer.WritePropertyName("f");
		JsonSerializer.Serialize(writer, value.Filter.SerializedBits, options);
		writer.WriteEndObject();
	}
}
