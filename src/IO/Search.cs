namespace Rope.IO;
using Rope;

public abstract record class Search()
{
	public abstract bool ShouldSearch(FileIndex index);

	public abstract IEnumerable<(long Start, long End)> SearchablePages(FileIndex index);

	public abstract bool Matches(Rope<string> values, Rope<string> headers);

    public static Search operator &(Search a, Search b) => new And(a, b);

    public static Search operator |(Search a, Search b) => new Or(a, b);
}
