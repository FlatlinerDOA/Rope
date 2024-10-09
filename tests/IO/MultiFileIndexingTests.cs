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
        // 188 MB of CSV Data from https://www.stats.govt.nz/large-datasets/csv-files-for-download/
        const string IndexPath = @"index.json";
        const string DataPath = @"../Data"; // @"D:\Datasets\nz_govt";
        var loadTime = Stopwatch.StartNew();

        var indexer = await CsvIndexer.LoadIndexFromJsonAsync(IndexPath);

        Debug.WriteLine($"Load took: {loadTime.ElapsedMilliseconds}ms");

        if (indexer is null)
        {
            indexer = new CsvIndexer();
            await indexer.IndexAllFilesInFolderAsync(DataPath, CancellationToken.None);
            await indexer.SaveIndexToJsonAsync(IndexPath);
        }
        
        var s = Stopwatch.StartNew();
        var data = new List<Dictionary<string, string>>();
        await foreach (var result in indexer.Search(DataPath, new ValueEquals("Year", "2022"), CancellationToken.None))
        {
            data.Add(result);
        }
        
        s.Stop();
        Debug.WriteLine($"Rows: {data.Count} found");
        Debug.WriteLine($"Search took: {s.ElapsedMilliseconds}ms");

        Assert.AreNotEqual(0, data.Count);
        var q = from row in data
                where row.ContainsKey("value")
                let value = decimal.TryParse(row.GetValueOrDefault("value"), out var value) ? value : 0m
                group value by row["industry_name_ANZSIC"] into g
                select (g.Key, g.Average());
    }
}