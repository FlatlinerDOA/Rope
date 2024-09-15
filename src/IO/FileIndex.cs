namespace Rope.IO;
using Rope;

public record class FileIndex(FileMetaData Meta, IReadOnlyDictionary<string, Rope<RowRange>> ColumnRanges)
{
	public string FilePath => this.Meta.Path;
}
