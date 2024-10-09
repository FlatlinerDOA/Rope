namespace Rope.IO;
using Rope;

public abstract record class Search()
{
	public abstract bool ShouldSearch(FileIndex index);

	public abstract IEnumerable<(long Start, long End, int StartRowIndex)> SearchablePages(FileIndex index);

	public abstract bool Matches(int rowIndex, Rope<string> values, Rope<string> headers);

    public static Search operator &(Search a, Search b) => new And(a, b);

    public static Search operator |(Search a, Search b) => new Or(a, b);
}

public record class RowsBetween(int Start, int End) : Search()
{
    public override IEnumerable<(long Start, long End, int StartRowIndex)> SearchablePages(FileIndex index) => from column in index.ColumnRanges
                                                                                            from page in column.Value
                                                                                            where page.EndRowIndex >= this.Start && page.StartRowIndex <= this.End
                                                                                            select (page.StartBytePosition, page.EndBytePosition, page.StartRowIndex);

    public override bool ShouldSearch(FileIndex index) => this.SearchablePages(index).Any();

    public override bool Matches(int rowIndex, Rope<string> values, Rope<string> headers) => this.Start <= rowIndex && this.End >= rowIndex;

}