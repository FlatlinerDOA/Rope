
namespace Rope.UnitTests.IO;

using Rope.IO;

[TestClass]
public class CsvIndexingTests
{
    [TestMethod]
    public async Task StartPositionIsCellsFirstCharacterIndexAndEndIsCommaIndex()
    {
        var indexer = new CsvIndexer();
        const string Source  = "Column1,Column2\r\nABC,DEF\r\n";
        var reader = new StringReader(Source);
        var indexedFile = await indexer.IndexFile("ABC", DateTime.UtcNow, reader);
        var firstCell = indexedFile.ColumnRanges["Column1"][0];
        Assert.AreEqual(Source[(int)firstCell.StartBytePosition..(int)firstCell.EndBytePosition], "ABC");
        Assert.AreEqual(18, firstCell.StartBytePosition);
        Assert.AreEqual(21, firstCell.EndBytePosition);
    }

    [TestMethod]
    public async Task StartPositionIsCellsFirstCharacterIndex()
    {
        var indexer = new CsvIndexer();
        var reader = new StringReader("Column1,Column2\r\nABC,DEF\r\n");
        var indexedFile = await indexer.IndexFile("ABC", DateTime.UtcNow, reader);
        Assert.AreEqual(18, indexedFile.ColumnRanges["Column1"][0].StartBytePosition);
        Assert.AreEqual(21, indexedFile.ColumnRanges["Column1"][0].EndBytePosition);
    }
}