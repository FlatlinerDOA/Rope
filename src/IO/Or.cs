namespace Rope.IO;
using Rope;

public record class Or(params Search[] Criteria) : Search
{
	public override bool ShouldSearch(FileIndex index) => this.Criteria.Any(c => c.ShouldSearch(index));

	public override IEnumerable<(long Start, long End)> SearchablePages(FileIndex index) =>
		(from c in this.Criteria
	    from range in c.SearchablePages(index)
		select range).Distinct().Order();

	public override bool Matches(Rope<string> values, Rope<string> headers) => Criteria.Any(c => c.Matches(values, headers));
}