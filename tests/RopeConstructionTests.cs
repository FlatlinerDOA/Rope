namespace Rope.UnitTests;

using System;
using System.Collections;
using System.Linq;

[TestClass]
public class RopeConstructionTests
{
    private static readonly string MediumText = new string(Enumerable.Range(0, 512).Select(i => (char)(36 + i % 24)).ToArray());

    [TestMethod]
    public void AblationOfConstructionSizes()
    {
        var sequence = Enumerable.Range(0, 12000).Select(c => (char)((int)'9' + (c % 26))).ToList();
        for (int chunkSize = 1; chunkSize < 100; chunkSize += 1)
        {
            var rope = sequence.Chunk(chunkSize).Select(chunk => new Rope<char>(chunk.ToArray())).Aggregate(Rope<char>.Empty, (prev, next) => prev + next);
            Assert.IsTrue(rope.SequenceEqual(sequence));
        }
    }

    [TestMethod]
    public void ConstructFromSingleElements()
    {
        var actual = new Rope<char>('A');
        Assert.AreEqual(1, actual.Length);
        Assert.AreEqual(0, actual.Depth);
        Assert.AreEqual(1, actual.Weight);
        Assert.IsFalse(actual.IsNode);
        Assert.IsTrue(actual.IsBalanced);
    }

    [TestMethod]
    public void ConstructFromSingleLengthMemory()
    {
        var actual = new Rope<char>(new[] { 'A' });
        Assert.AreEqual(1, actual.Length);
        Assert.AreEqual(0, actual.Depth);
        Assert.AreEqual(1, actual.Weight);
        Assert.IsFalse(actual.IsNode);
        Assert.IsTrue(actual.IsBalanced);
    }

    [TestMethod]
    public void ConstructWithRightEmpty()
    {
        var actual = new Rope<char>("test".ToRope(), Rope<char>.Empty);
        Assert.IsFalse(actual.IsNode);
        Assert.AreEqual(4, actual.Length);
        Assert.AreEqual("test".ToRope(), actual);
    }

    [TestMethod]
    public void ConstructWithRightEmptyLeftNode()
    {
        var actual = new Rope<char>("te".ToRope() + "st".ToRope(), Rope<char>.Empty);

        Assert.IsTrue(actual.IsNode);
        Assert.IsTrue(actual.IsBalanced);
        Assert.AreEqual(4, actual.Length);
        Assert.AreEqual("test".ToRope(), actual);


        actual = new Rope<char>(MediumText[..256].ToRope() + MediumText[256..].ToRope(), Rope<char>.Empty);
        Assert.IsTrue(actual.IsNode);
        Assert.IsTrue(actual.IsBalanced);
        Assert.AreEqual(MediumText.Length, actual.Length);
        Assert.AreEqual(MediumText, actual);
    }

    [TestMethod]
    public void ConstructWithLeftEmpty()
    {
        var actual = new Rope<char>(Rope<char>.Empty, "test".ToRope());
        Assert.IsFalse(actual.IsNode);
        Assert.AreEqual(4, actual.Length);
        Assert.AreEqual("test".ToRope(), actual);
    }

    [TestMethod]
    public void ConstructWithLeftEmptyRightNode()
    {
        var actual = new Rope<char>(Rope<char>.Empty, "te".ToRope() + "st".ToRope());

        Assert.IsTrue(actual.IsNode);
        Assert.IsTrue(actual.IsBalanced);
        Assert.AreEqual(4, actual.Length);
        Assert.AreEqual("test".ToRope(), actual);

        actual = new Rope<char>(Rope<char>.Empty, MediumText[..256].ToRope() + MediumText[256..].ToRope());
        Assert.IsTrue(actual.IsNode);
        Assert.IsTrue(actual.IsBalanced);
        Assert.AreEqual(MediumText, actual);
    }

    [TestMethod]
    public void AddOperatorTwoRopes() => Assert.AreEqual("Lorem ipsum", ("Lorem ".ToRope() + "ipsum".ToRope()).ToString());

    [TestMethod]
    public void AddOperatorSingleElement() => Assert.AreEqual("Lorem ipsum", ("Lorem ipsu".ToRope() + 'm').ToString());

    [TestMethod]
    public void LargeAddEqualsOperator()
    {
        var s = Rope<char>.Empty;
        for (int i = 0; i < 1000; i++)
        {
            s += TestData.LargeText.ToRope();
        }

        Assert.AreEqual(s.Length, TestData.LargeText.Length * 1000);
    }

    [TestMethod]
    public void ConcatRopes() => Assert.AreEqual("Lorem ipsum", "Lorem ".ToRope().AddRange("ipsum".ToRope()).ToString());

    [TestMethod]
    public void ConcatAndEnumerateChars() => Assert.IsTrue(("The ghosts say ".ToRope() + "boo dee boo".ToRope()).SequenceEqual("The ghosts say boo dee boo"));

    [TestMethod]
    public void ConcatAndEnumerateObjects() => Assert.IsTrue(((IEnumerable)("The ghosts say ".ToRope() + "boo dee boo".ToRope())).Cast<char>().SequenceEqual("The ghosts say boo dee boo"));

    [TestMethod]
    public void ImplicitFromArray()
    {
        var chars = new[] { 'I', '\'', 'm', ' ', 's', 'o', 'r', 'r', 'y', ' ', 'D', 'a', 'v', 'e', '.' };
        Rope<char> r = chars;
        Assert.IsTrue(r.SequenceEqual(chars));
    }

    [TestMethod]
    public void CombineRopeOfRopes()
    {
        var ropeOfRopes = new[] { "te".ToRope(), "st".ToRope() }.ToRope();
        var actual = ropeOfRopes.Combine();
        Assert.AreEqual("test".ToRope(), actual);
    }

    [TestMethod]
    public void CollectionInitializer()
    {
        Rope<char> chars = ['H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', '!'];
        Assert.IsTrue(chars.SequenceEqual(['H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', '!']));
    }
}
