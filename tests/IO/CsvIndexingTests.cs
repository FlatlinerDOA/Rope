
namespace Rope.UnitTests.IO;

using System.Text;
using Rope.IO;

[TestClass]
public class CsvIndexingTests
{
    private const string SingleLineCsv  = "Column1,Column2\r\nABC,DEF\r\n";

    private const string MultiLineCsv = "Column1,Column2\r\nABC,DEF\r\nABC,XYZ\r\n";

    [TestMethod]
    public async Task ParseRangeIsRowStartToRowEnd()
    {
        var indexer = new CsvIndexer();
        var reader = new MemoryStream(Encoding.UTF8.GetBytes(SingleLineCsv));
        var indexedFile = await indexer.IndexFileAsync("File1.csv", DateTime.UtcNow, reader);
        var firstCell = indexedFile.ColumnRanges["Column1"][0];
        Assert.AreEqual(SingleLineCsv[(int)firstCell.StartBytePosition..(int)firstCell.EndBytePosition], "ABC,DEF\r\n");
        Assert.AreEqual("Column1,Column2\r\n".Length, firstCell.StartBytePosition);
        Assert.AreEqual("Column1,Column2\r\n".Length + "ABC,DEF\r\n".Length, firstCell.EndBytePosition);

        var secondCell = indexedFile.ColumnRanges["Column2"][0];
        Assert.AreEqual(firstCell.StartBytePosition, secondCell.StartBytePosition);
        Assert.AreEqual(firstCell.EndBytePosition, secondCell.EndBytePosition);
    }

    [TestMethod]
    public void ReadLineConsumesLineFeed()
    {
        var reader = new StringReader(SingleLineCsv);
        var header = reader.ReadCsvLine(2, out var consumed);
        Assert.AreEqual("Column1", header[0]);
        Assert.AreEqual("Column2", header[1]);
        Assert.AreEqual("Column1,Column2\r\n".Length, consumed);

        var line = reader.ReadCsvLine(2, out consumed);
        Assert.AreEqual("ABC", line[0]);
        Assert.AreEqual("DEF", line[1]);
        Assert.AreEqual("ABC,DEF\r\n".Length, consumed);
    }

    [TestMethod]
    public async Task IndexingShouldIncludeInSearchResults()
    {
        var indexer = new CsvIndexer()
        {
            EnumerateFiles = folder => ["File1.csv"],
            ReadFile = f => (new MemoryStream(Encoding.UTF8.GetBytes(SingleLineCsv)), new DateTime(2024, 10, 6, 19, 0, 0))
        };

        await indexer.IndexFileAsync("File1.csv");
        int resultCount = 0;
        await foreach (var result in indexer.Search(".", new ValueStartsWith("Column1", "ABC"), CancellationToken.None))
        {
            Assert.AreEqual("ABC", result["Column1"]);
            resultCount++;
        }

        Assert.AreEqual(1, resultCount);
    }


    [TestMethod]
    public async Task AndSearchShouldGiveOneResult()
    {
        var indexer = new CsvIndexer()
        {
            EnumerateFiles = folder => ["File1.csv"],
            ReadFile = f => (new MemoryStream(Encoding.UTF8.GetBytes(MultiLineCsv)), new DateTime(2024, 10, 6, 19, 0, 0)),
            RowsPerPage = 1
        };

        await indexer.IndexFileAsync("File1.csv");
        int resultCount = 0;
        await foreach (var result in indexer.Search(".", new ValueStartsWith("Column1", "ABC") & new ValueEquals("Column2", "DEF"), CancellationToken.None))
        {
            Assert.AreEqual("ABC", result["Column1"]);
            resultCount++;
        }

        Assert.AreEqual(1, resultCount);
    }

    [TestMethod]
    public async Task OrSearchShouldGiveTwoResults()
    {
        var indexer = new CsvIndexer()
        {
            EnumerateFiles = folder => ["File1.csv"],
            ReadFile = f => (new MemoryStream(Encoding.UTF8.GetBytes(MultiLineCsv)), new DateTime(2024, 10, 6, 19, 0, 0)),
            RowsPerPage = 1
        };

        await indexer.IndexFileAsync("File1.csv");
        int resultCount = 0;
        await foreach (var result in indexer.Search(".", new ValueStartsWith("Column2", "DEF") | new ValueEquals("Column2", "XYZ"), CancellationToken.None))
        {
            Assert.AreEqual("ABC", result["Column1"]);
            Assert.IsTrue(new[] { "DEF", "XYZ" }.Contains(result["Column2"]));
            resultCount++;
        }

        Assert.AreEqual(2, resultCount);
    }
}