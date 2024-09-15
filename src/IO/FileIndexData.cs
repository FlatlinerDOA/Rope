namespace Rope.IO;
using System.Text.Json.Serialization;
using Rope;

public record class FileIndexData(string FilePath, DateTimeOffset LastModifiedUtc, ColumnIndexData[] Columns)
{
	[JsonIgnore]
	public Rope<string> Headers => this.Columns.Select(c => c.Name).ToRope();
};
