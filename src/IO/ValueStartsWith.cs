namespace Rope.IO;
using Rope;

public record class ValueStartsWith(string Column, string Value) : Search()
{
	public override bool ShouldSearch(FileIndex index) => index.ColumnRanges.ContainsKey(this.Column);

	public override IEnumerable<(long Start, long End)> SearchablePages(FileIndex index) => index.ColumnRanges[this.Column].Where(r => r.Filter.MightStartWith(this.Value)).Select(r => (r.StartBytePosition, r.EndBytePosition));

	public override bool Matches(Rope<string> values, Rope<string> headers)
	{
		var c = headers.IndexOf(this.Column);
		return c >= 0 && c < values.Count && this.Matches(values[c]);
	}
	
	public bool Matches(string value) => value.StartsWith(this.Value);
}
