namespace Rope.UnitTests;

[TestClass]
public class RopeEditingTests
{
    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(1, 1, 0)]
    [DataRow(1, 1, 1)]
    [DataRow(2, 1, 0)]
    [DataRow(2, 1, 1)]
    [DataRow(2, 1, 2)]
    [DataRow(2, 1, 0)]
    [DataRow(2, 2, 1)]
    [DataRow(2, 2, 2)]
    [DataRow(3, 2, 2)]
    [DataRow(5, 2, 0)]
    [DataRow(5, 2, 2)]
    [DataRow(5, 2, 3)]
    [DataRow(5, 2, 5)]
    [DataRow(5, 5, 5)]
    [DataRow(5, 5, 4)]
    [DataRow(256, 3, 4)]
    [DataRow(256, 24, 3)]
    public void SliceStart(int textLength, int chunkSize, int startIndex)
    {
        var (text, rope) = TestData.Create(textLength, chunkSize);
        var expected = new string(text.AsSpan().Slice(startIndex));
        var actual = rope.Slice(startIndex);
        Assert.AreEqual(expected, actual.ToString());
    }

    [TestMethod]
    [DataRow(0, 0, 0, 0)]
    [DataRow(1, 1, 0, 1)]
    [DataRow(1, 1, 1, 0)]
    [DataRow(2, 1, 0, 1)]
    [DataRow(2, 1, 1, 1)]
    [DataRow(2, 1, 2, 0)]
    [DataRow(2, 1, 0, 2)]
    [DataRow(2, 2, 1, 1)]
    [DataRow(2, 2, 2, 0)]
    [DataRow(3, 2, 2, 1)]
    [DataRow(5, 2, 0, 5)]
    [DataRow(5, 2, 2, 3)]
    [DataRow(5, 2, 3, 2)]
    [DataRow(5, 2, 4, 1)]
    [DataRow(5, 2, 5, 0)]
    [DataRow(5, 5, 4, 1)]
    [DataRow(256, 3, 4, 200)]
    [DataRow(256, 24, 3, 200)]
    public void SliceStartAndLength(int textLength, int chunkSize, int startIndex, int length)
    {
        var (text, rope) = TestData.Create(textLength, chunkSize);
        var expected = new string(text.AsSpan().Slice(startIndex, length));
        var actual = rope.Slice(startIndex, length);
        Assert.AreEqual(expected, actual.ToString());
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(1, 1, 0)]
    [DataRow(1, 1, 1)]
    [DataRow(2, 1, 0)]
    [DataRow(2, 1, 1)]
    [DataRow(2, 1, 2)]
    [DataRow(2, 1, 0)]
    [DataRow(2, 2, 1)]
    [DataRow(2, 2, 2)]
    [DataRow(3, 2, 2)]
    [DataRow(5, 2, 0)]
    [DataRow(5, 2, 2)]
    [DataRow(5, 2, 3)]
    [DataRow(5, 2, 5)]
    [DataRow(256, 3, 4)]
    [DataRow(256, 24, 3)]
    public void SplitAt(int textLength, int chunkSize, int startIndex)
    {
        var (text, rope) = TestData.Create(textLength, chunkSize);
        var (expectedLeft, expectedRight) = (text[..startIndex], text[startIndex..]);
        var (actualLeft, actualRight) = rope.SplitAt(startIndex);
        Assert.AreEqual(expectedLeft, actualLeft.ToString());
        Assert.AreEqual(expectedRight, actualRight.ToString());
    }

    [TestMethod]
    public void AblationOfSplitAt()
    {
        var sequence = Enumerable.Range(0, 1200).Select(c => (char)((int)'9' + (c % 26))).ToList();
        for (int chunkSize = 1; chunkSize < 100; chunkSize += 1)
        {
            var rope = sequence.Chunk(chunkSize).Select(chunk => new Rope<char>(chunk.ToArray())).Aggregate(Rope<char>.Empty, (prev, next) => prev + next);
            for (int splitPoint = 0; splitPoint < rope.Length; splitPoint++)
            {
                var (left, right) = rope.SplitAt(splitPoint);
                Assert.AreEqual(splitPoint, left.Length);
                Assert.AreEqual(rope.Length - splitPoint, right.Length);
            }
        }
    }

    [TestMethod]
    public void ElementIndexerBeforeBreak() => Assert.AreEqual("this is a test"[3], ("this".ToRope() + " is a test.".ToRope())[3]);

    [TestMethod]
    public void ElementIndexerAfterBreak() => Assert.AreEqual("this is a test"[4], ("this".ToRope() + " is a test.".ToRope())[4]);

    [TestMethod]
    public void RangeIndexer() => Assert.AreEqual("this is a test."[3..8].ToRope(), ("this".ToRope() + " is a test.".ToRope())[3..8]);

    [TestMethod]
    public void RangeEndIndexer() => Assert.AreEqual("this is a test."[3..^3].ToRope(), ("this".ToRope() + " is a test.".ToRope())[3..^3]);

    [TestMethod]
    public void SplitBySingleElement() => Assert.IsTrue("this is a test of the things I split by.".ToRope().Split(' ').Select(c => c.ToString()).SequenceEqual("this is a test of the things I split by.".Split(' ')));

    [TestMethod]
    public void SplitBySingleElementNotFound() => Assert.IsTrue("this is a test of the things I split by.".ToRope().Split('_').Select(c => c.ToString()).SequenceEqual(new[] { "this is a test of the things I split by." }));

    [TestMethod]
    public void SplitBySingleElementAtEnd() => Assert.IsTrue("this is a test of the things I split by.".ToRope().Split('.').Select(c => c.ToString()).SequenceEqual("this is a test of the things I split by.".Split('.')));

    [TestMethod]
    public void SplitBySequence() => Assert.IsTrue("this  is  a  test  of  the  things  I  split  by.".ToRope().Split("  ".AsMemory()).Select(c => c.ToString()).SequenceEqual("this  is  a  test  of  the  things  I  split  by.".Split("  ")));

    [TestMethod]
    public void Replace() => Assert.AreEqual(
        "The ghosts say boo dee boo",
        "The ghosts say doo dee doo".ToRope().Replace("doo".AsMemory(), "boo".AsMemory()).ToString());

    [TestMethod]
    public void ReplaceSplit() => Assert.AreEqual(
        "this is a test".Replace(" is ", " isn't "),
        ("this i".ToRope() + "s a test".ToRope()).Replace(" is ", " isn't ").ToString());

    [TestMethod]
    public void InsertRope()
    {
        var s = TestData.LargeText;
        for (int i = 0; i < 1000; i++)
        {
            s = s.InsertRange(TestData.LargeText.Length / 2, TestData.LargeText);
        }

        Assert.AreEqual(TestData.LargeText.Length * 1001, s.Length);
    }

    [TestMethod]
    public void InsertMemory()
    {
        var memory = TestData.LargeText.ToMemory();
        var s = TestData.LargeText;
        for (int i = 0; i < 1000; i++)
        {
            s = s.InsertRange(memory.Length / 2, memory);
        }

        Assert.AreEqual(memory.Length * 1001, s.Length);
    }

    [TestMethod]
    public void InsertSortedLargeFloatList()
    {
        var random = new Random(42);
        var rope = Rope<float>.Empty;
        var comparer = Comparer<float>.Default;
        foreach (var rank in Enumerable.Range(0, 65000).Select(s => (float)random.NextDouble()))
        {
            rope = rope.InsertSorted(rank, comparer);
        }
    }

    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(1, 1, 1)]
    [DataRow(2, 1, 2)]
    [DataRow(2, 2, 2)]
    [DataRow(5, 2, 5)]
    [DataRow(5, 5, 5)]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void RemoveAtOutOfRange(int textLength, int chunkSize, int startIndex)
    {
        var (_, rope) = TestData.Create(textLength, chunkSize);
        var _ = rope.RemoveAt(startIndex);
    }

    [TestMethod]
    [DataRow(1, 1, 0)]
    [DataRow(2, 1, 0)]
    [DataRow(2, 1, 1)]
    [DataRow(2, 2, 1)]
    [DataRow(3, 2, 2)]
    [DataRow(5, 2, 0)]
    [DataRow(5, 2, 2)]
    [DataRow(5, 2, 3)]
    [DataRow(5, 5, 4)]
    [DataRow(256, 3, 4)]
    [DataRow(256, 24, 3)]
    public void RemoveAt(int textLength, int chunkSize, int startIndex)
    {
        var (text, rope) = TestData.Create(textLength, chunkSize);
        var list = text.ToList();
        list.RemoveAt(startIndex);
        var expected = new string(list.ToArray());
        var actual = rope.RemoveAt(startIndex);
        Assert.AreEqual(expected, actual.ToString());
    }

    [TestMethod]
    [DataRow(0, 0, 0, 0)]
    [DataRow(1, 1, 0, 1)]
    [DataRow(1, 1, 1, 0)]
    [DataRow(2, 1, 0, 1)]
    [DataRow(2, 1, 1, 1)]
    [DataRow(2, 1, 2, 0)]
    [DataRow(2, 1, 0, 2)]
    [DataRow(2, 2, 1, 1)]
    [DataRow(2, 2, 2, 0)]
    [DataRow(3, 2, 2, 1)]
    [DataRow(5, 2, 0, 5)]
    [DataRow(5, 2, 2, 3)]
    [DataRow(5, 2, 3, 2)]
    [DataRow(5, 2, 4, 1)]
    [DataRow(5, 2, 5, 0)]
    [DataRow(5, 5, 4, 1)]
    [DataRow(5, 5, 5, 0)]
    [DataRow(256, 3, 4, 200)]
    [DataRow(256, 24, 3, 200)]
    public void RemoveRange(int textLength, int chunkSize, int startIndex, int length)
    {
        var (text, rope) = TestData.Create(textLength, chunkSize);
        var expected = text.Remove(startIndex, length);
        var actual = rope.RemoveRange(startIndex, length);
        Assert.AreEqual(expected, actual.ToString());
    }
}
