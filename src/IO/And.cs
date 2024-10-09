namespace Rope.IO;
using Rope;

public record class And(params Search[] Criteria) : Search 
{
	public override bool ShouldSearch(FileIndex index) => this.Criteria.All(c => c.ShouldSearch(index));

	public override IEnumerable<(long Start, long End, int StartRowIndex)> SearchablePages(FileIndex index) =>
		from c in this.Criteria
	    from range in c.SearchablePages(index)
		group c by range into g
		where !this.Criteria.Except(g).Any()
		orderby g.Key
		select g.Key;

    public override bool Matches(int rowIndex, Rope<string> values, Rope<string> headers) => this.Criteria.All(c => c.Matches(rowIndex, values, headers));
}
