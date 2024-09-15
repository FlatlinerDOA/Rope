namespace Rope.IO;
using Rope;

public record class Search(string Column, string Value, bool StartsWith = false)
{
	public bool Matches(Rope<string> values, Rope<string> headers)
	{
		var c = headers.IndexOf(this.Column);
		return c >= 0 && c < values.Count && this.Matches(values[c]);
	}
	
	public bool Matches(string value) => StartsWith ? value.StartsWith(this.Value) : value == this.Value;
}
