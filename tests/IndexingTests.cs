namespace Rope.UnitTests;

using System.Diagnostics;
using Rope.IO;

public class IndexingTests
{
    public void IndexCsvs()
    {
        // Data from https://www.stats.govt.nz/large-datasets/csv-files-for-download/
        var filePaths = Directory.EnumerateFiles(@"..\Data", "*.csv", new EnumerationOptions() { RecurseSubdirectories = true }).ToList();
        const string IndexPath = @"index.json";
        
        CsvIndexer indexer;
        if (!File.Exists(IndexPath))
        {
            indexer = new CsvIndexer();
            indexer.IndexCsvFiles(filePaths);
            indexer.SaveIndexToJson(IndexPath);
        }
        else
        {
            indexer = CsvIndexer.LoadIndexFromJson(IndexPath);
        }
        
        var s = Stopwatch.StartNew();
        var data = indexer.Search(new Search("year", "2022"), new Search("unit", "DOLLARS(millions)")).ToList();
        
        s.Stop();
        Debug.WriteLine(s.ElapsedMilliseconds);
        
        var q = from row in data
                where row.ContainsKey("value")
                let value = decimal.TryParse(row.GetValueOrDefault("value"), out var value) ? value : 0m
                group value by row["industry_name_ANZSIC"] into g
                select (g.Key, g.Average());
    }
}