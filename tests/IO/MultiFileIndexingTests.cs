namespace Rope.UnitTests.IO;

using System.Diagnostics;
using Rope.IO;

[TestClass]
public class MultiFileIndexingTests
{
    [TestMethod]
    [Ignore]
    public async Task IndexCsvs()
    {
        // Data from https://www.stats.govt.nz/large-datasets/csv-files-for-download/
        const string IndexPath = @"index.json";
        
        CsvIndexer indexer;
        if (!File.Exists(IndexPath))
        {
            indexer = new CsvIndexer();
            await indexer.IndexAllFilesInFolderAsync(@"..\Data", CancellationToken.None);
            await indexer.SaveIndexToJson(IndexPath);
        }
        else
        {
            indexer = CsvIndexer.LoadIndexFromJson(IndexPath);
        }
        
        var s = Stopwatch.StartNew();
        var data = new List<Dictionary<string, string>>();
        await foreach (var result in indexer.Search(@"..\Data", new And(new ValueEquals("year", "2022"), new ValueEquals("unit", "DOLLARS(millions)")), CancellationToken.None))
        {

        }
        
        s.Stop();
        Debug.WriteLine(s.ElapsedMilliseconds);
        
        var q = from row in data
                where row.ContainsKey("value")
                let value = decimal.TryParse(row.GetValueOrDefault("value"), out var value) ? value : 0m
                group value by row["industry_name_ANZSIC"] into g
                select (g.Key, g.Average());
    }
}