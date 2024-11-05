namespace Rope.UnitTests;

using Rope.Compare;
using System;

[TestClass]
public class RopeSearchTests
{
    [TestMethod]
    public void IndexOfRope()
    {
        Assert.AreEqual("y".IndexOf("y"), "y".ToRope().IndexOf("y".AsMemory()));
        Assert.AreEqual("def abcdefgh".IndexOf("def"), new Rope<char>("def abcd".ToRope(), "efgh".ToRope()).IndexOf("def".ToRope()));

        Assert.AreEqual(0, "test".ToRope().IndexOf(new Rope<char>("test".ToRope(), Rope<char>.Empty)));
        Assert.AreEqual(0, new Rope<char>("test".ToRope(), Rope<char>.Empty).IndexOf("test".ToRope()));

        Assert.AreEqual(0, "test".ToRope().IndexOf(new Rope<char>("te".ToRope(), "st".ToRope())));
        Assert.AreEqual(0, new Rope<char>("tes".ToRope(), "t".ToRope()).IndexOf("test".ToRope()));

        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".IndexOf("ed over a"),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").IndexOf("ed over a".ToRope()));

        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".IndexOf("he quick"),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").IndexOf("he quick".ToRope()));
    }

    [TestMethod]
    public void IndexOfRopeAfter()
    {
        Assert.AreEqual("abc abc".IndexOf("bc", 2), "abc abc".ToRope().IndexOf("bc".ToRope(), 2));
        Assert.AreEqual("abc abc".IndexOf("c", 2), "abc abc".ToRope().IndexOf("c".AsMemory(), 2));
        Assert.AreEqual("abc abc".IndexOf("c", 2), "abc abc".ToRope().IndexOf("c".ToRope(), 2));
        Assert.AreEqual("abc abc".IndexOf("c", 6), "abc abc".ToRope().IndexOf("c".ToRope(), 6));
        Assert.AreEqual("abc abc".IndexOf("c", 7), "abc abc".ToRope().IndexOf("c".ToRope(), 7));
        Assert.AreEqual("ABC".IndexOf("B", 0), "ABC".ToRope().IndexOf("B".ToRope(), 0));
        Assert.AreEqual("ABC".IndexOf("B", 1), "ABC".ToRope().IndexOf("B".ToRope(), 1));
        Assert.AreEqual("ABC".IndexOf("C", 2), "ABC".ToRope().IndexOf("C".ToRope(), 2));

        Assert.AreEqual("ABC".IndexOf("B", 0), new Rope<char>("A".ToRope(), "BC".ToRope()).IndexOf("B".ToRope(), 0));
        Assert.AreEqual("ABC".IndexOf("B", 1), new Rope<char>("A".ToRope(), "BC".ToRope()).IndexOf("B".ToRope(), 1));
        Assert.AreEqual("ABC".IndexOf("C", 2), new Rope<char>("A".ToRope(), "BC".ToRope()).IndexOf("C".ToRope(), 2));
        Assert.AreEqual("ab".IndexOf("ab", 1), new Rope<char>("a".ToRope(), "b".ToRope()).IndexOf("ab".ToRope(), 1));
        Assert.AreEqual("ab".IndexOf(string.Empty, 1), new Rope<char>("a".ToRope(), "b".ToRope()).IndexOf(Rope<char>.Empty, 1));
        Assert.AreEqual("ab".IndexOf(string.Empty, 2), new Rope<char>("a".ToRope(), "b".ToRope()).IndexOf(Rope<char>.Empty, 2));
    }

    [TestMethod]
    public void IndexOfElement()
    {
        Assert.AreEqual("y".IndexOf('y'), "y".ToRope().IndexOf('y'));
        Assert.AreEqual("def abcdefgh".IndexOf('e'), new Rope<char>("def abcd".ToRope(), "efgh".ToRope()).IndexOf('e'));
        Assert.AreEqual(0, "test".ToRope().IndexOf(new Rope<char>(new Rope<char>('t'), Rope<char>.Empty)));
        Assert.AreEqual(0, new Rope<char>(new Rope<char>('t'), Rope<char>.Empty).IndexOf('t'));
        Assert.AreEqual(0, new Rope<char>("tes".ToRope(), "t".ToRope()).IndexOf('t'));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".IndexOf('e'),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").IndexOf('e'));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".IndexOf('h'),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").IndexOf('h'));
    }

    [TestMethod]
    public void IndexOfElementAfter()
    {
        Assert.AreEqual("abc abc".IndexOf('b', 2), "abc abc".ToRope().IndexOf('b', 2));
        Assert.AreEqual("abc abc".IndexOf('c', 2), "abc abc".ToRope().IndexOf('c', 2));
        Assert.AreEqual("abc abc".IndexOf('c', 6), "abc abc".ToRope().IndexOf('c', 6));
        Assert.AreEqual("abc abc".IndexOf('c', 7), "abc abc".ToRope().IndexOf('c', 7));
        Assert.AreEqual("ABC".IndexOf('B', 0), "ABC".ToRope().IndexOf('B', 0));
        Assert.AreEqual("ABC".IndexOf('B', 1), "ABC".ToRope().IndexOf('B', 1));
        Assert.AreEqual("ABC".IndexOf('C', 2), "ABC".ToRope().IndexOf('C', 2));

        Assert.AreEqual("ABC".IndexOf('B', 0), new Rope<char>("A".ToRope(), "BC".ToRope()).IndexOf('B', 0));
        Assert.AreEqual("ABC".IndexOf('B', 1), new Rope<char>("A".ToRope(), "BC".ToRope()).IndexOf('B', 1));
        Assert.AreEqual("ABC".IndexOf('C', 2), new Rope<char>("A".ToRope(), "BC".ToRope()).IndexOf('C', 2));
        Assert.AreEqual("ab".IndexOf('a', 1), new Rope<char>("a".ToRope(), "b".ToRope()).IndexOf('a', 1));
        Assert.AreEqual(string.Empty.IndexOf('a', 0), Rope<char>.Empty.IndexOf('a', 0));
    }

    [TestMethod]
    public void IndexOfInOverlap() => Assert.AreEqual(4, ("abcdef".ToRope() + "ghijklm".ToRope()).IndexOf("efgh".ToRope()));

    [TestMethod]
    public void IndexOfInRight() => Assert.AreEqual(6, ("abcdef".ToRope() + "ghijklm".ToRope()).IndexOf("ghi".ToRope()));

    [TestMethod]
    public void IndexOfAfter() => Assert.AreEqual("abc abc".IndexOf('c', 3), "abc abc".ToRope().IndexOf('c', 3));

    [TestMethod]
    public void ConcattedIndexOfElement() => Assert.AreEqual("abc def".IndexOf('d'), ("ab".ToRope() + "c def".ToRope()).IndexOf('d'));

    [TestMethod]
    public void ConcattedIndexOfMemory() => Assert.AreEqual("abc abc".IndexOf("bc "), ("ab".ToRope() + "c abc".ToRope()).IndexOf("bc ".AsMemory()));

    [TestMethod]
    public void ConcattedIndexOfAfter() => Assert.AreEqual("abc abc".IndexOf("bc ", 1), ("ab".ToRope() + "c abc".ToRope()).IndexOf("bc ".AsMemory(), 1));

    [TestMethod]
    public void LastIndexOfRope()
    {
        Assert.AreEqual("y".LastIndexOf('y'), "y".ToRope().LastIndexOf("y".AsMemory()));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".LastIndexOf("ed over a"),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").LastIndexOf("ed over a".ToRope()));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".LastIndexOf("he quick"),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").LastIndexOf("he quick".ToRope()));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".LastIndexOf(" "),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").LastIndexOf(" ".ToRope()));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".LastIndexOf("Th"),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").LastIndexOf("Th".ToRope()));
        Assert.AreEqual(
            "The quick brown fox jumped over a lazy dog.".LastIndexOf("."),
            ("Th".ToRope() + "e" + " quick brown fox jumped over a lazy dog.").LastIndexOf(".".ToRope()));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void LastIndexOfElementWithStartIndexOutOfBounds()
    {
        _ = "0123456789".ToRope().LastIndexOf('9', 10);
    }

    [TestMethod]
    public void LastIndexOfElement()
    {
        Assert.AreEqual("y".LastIndexOf('y'), "y".ToRope().LastIndexOf('y'));
        Assert.AreEqual("abc abc".LastIndexOf('c'), "abc abc".ToRope().LastIndexOf('c'));
        Assert.AreEqual("abc abc".LastIndexOf('a'), new Rope<char>("abc a".ToRope(), "bc".ToRope()).LastIndexOf('a'));
        Assert.AreEqual("abc abc".LastIndexOf('a'), new Rope<char>("abc ".ToRope(), "abc".ToRope()).LastIndexOf('a'));
        Assert.AreEqual("abc abc".LastIndexOf('x'), new Rope<char>("abc ".ToRope(), "abc".ToRope()).LastIndexOf('x'));
    }

    [TestMethod]
    public void LastIndexOfElementWithStartIndex()
    {
        Assert.AreEqual("abc abc".LastIndexOf('c', 2), "abc abc".ToRope().LastIndexOf('c', 2));
        Assert.AreEqual("0123456789".LastIndexOf('9', 9), "0123456789".ToRope().LastIndexOf('9', 9));
    }

    [TestMethod]
    public void LastIndexOfRopeWithStartIndex()
    {
        Assert.AreEqual("abc abc".LastIndexOf("c", 2), "abc abc".ToRope().LastIndexOf("c".ToRope(), 2));
        Assert.AreEqual("abc abc".LastIndexOf("c", 6), "abc abc".ToRope().LastIndexOf("c".ToRope(), 6));
        Assert.AreEqual("abc abc".LastIndexOf("c", 7), "abc abc".ToRope().LastIndexOf("c".ToRope(), 7));
        Assert.AreEqual("ABC".LastIndexOf("B", 0), "ABC".ToRope().LastIndexOf("B".ToRope(), 0));
        Assert.AreEqual("ABC".LastIndexOf("B", 1), "ABC".ToRope().LastIndexOf("B".ToRope(), 1));
        Assert.AreEqual("ABC".LastIndexOf("C", 2), "ABC".ToRope().LastIndexOf("C".ToRope(), 2));

        Assert.AreEqual("ABC".LastIndexOf("B", 0), new Rope<char>("A".ToRope(), "BC".ToRope()).LastIndexOf("B".ToRope(), 0));
        Assert.AreEqual("ABC".LastIndexOf("B", 1), new Rope<char>("A".ToRope(), "BC".ToRope()).LastIndexOf("B".ToRope(), 1));
        Assert.AreEqual("ABC".LastIndexOf("C", 2), new Rope<char>("A".ToRope(), "BC".ToRope()).LastIndexOf("C".ToRope(), 2));
        Assert.AreEqual("ab".LastIndexOf("ab", 1), new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf("ab".ToRope(), 1));
        Assert.AreEqual("def abcdefgh".LastIndexOf("def"), new Rope<char>("def abcd".ToRope(), "efgh".ToRope()).LastIndexOf("def".ToRope()));
        Assert.AreEqual("abc abc".LastIndexOf("bc", 2), ("ab".ToRope() + "c abc".ToRope()).LastIndexOf("bc".AsMemory(), 2));
    }

    [TestMethod]
    public void LastIndexOfRopeWithStartIndexAndEmptyString()
    {
#if NET8_0_OR_GREATER
        var expectedA = "ab".LastIndexOf(string.Empty, 1);
        var expectedB = "ab".LastIndexOf(string.Empty, 2);
#else
        // Apparently there was a breaking change for string handling between NET Core 3.0 and NET 5.0 - https://github.com/dotnet/runtime/issues/43736
        var expectedA = 2;
        var expectedB = 2;
#endif
        Assert.AreEqual(expectedA, new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf(Rope<char>.Empty, 1));
        Assert.AreEqual(expectedB, new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf(Rope<char>.Empty, 2));
    }

    [TestMethod]
    public void LastIndexOfRopeWithStartIndexIgnoreCase()
    {
        var comparer = CharComparer.OrdinalIgnoreCase;

        Assert.AreEqual("abc abC".LastIndexOf("c", 2, StringComparison.OrdinalIgnoreCase), "abc abC".ToRope().LastIndexOf("c".ToRope(), 2, comparer));
        Assert.AreEqual("abc abC".LastIndexOf("c", 6, StringComparison.OrdinalIgnoreCase), "abc abC".ToRope().LastIndexOf("c".ToRope(), 6, comparer));
        Assert.AreEqual("abc abC".LastIndexOf("c", 7, StringComparison.OrdinalIgnoreCase), "abc abC".ToRope().LastIndexOf("c".ToRope(), 7, comparer));
        Assert.AreEqual("ABC".LastIndexOf("B", 0, StringComparison.OrdinalIgnoreCase), "ABC".ToRope().LastIndexOf("B".ToRope(), 0, comparer));
        Assert.AreEqual("ABC".LastIndexOf("B", 1, StringComparison.OrdinalIgnoreCase), "ABC".ToRope().LastIndexOf("B".ToRope(), 1, comparer));
        Assert.AreEqual("ABC".LastIndexOf("C", 2, StringComparison.OrdinalIgnoreCase), "ABC".ToRope().LastIndexOf("C".ToRope(), 2, comparer));
        Assert.AreEqual("C".LastIndexOf("c", 0, StringComparison.OrdinalIgnoreCase), "C".ToRope().LastIndexOf("c".ToRope(), 0, comparer));

        Assert.AreEqual("ABC".LastIndexOf("B", 0, StringComparison.OrdinalIgnoreCase), new Rope<char>("A".ToRope(), "BC".ToRope()).LastIndexOf("B".ToRope(), 0, comparer));
        Assert.AreEqual("ABC".LastIndexOf("B", 1, StringComparison.OrdinalIgnoreCase), new Rope<char>("A".ToRope(), "BC".ToRope()).LastIndexOf("B".ToRope(), 1, comparer));
        Assert.AreEqual("ABC".LastIndexOf("C", 2, StringComparison.OrdinalIgnoreCase), new Rope<char>("A".ToRope(), "BC".ToRope()).LastIndexOf("C".ToRope(), 2, comparer));
        Assert.AreEqual("ab".LastIndexOf("ab", 1, StringComparison.OrdinalIgnoreCase), new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf("ab".ToRope(), 1, comparer));
        Assert.AreEqual("def abcdefgh".LastIndexOf("def", StringComparison.OrdinalIgnoreCase), new Rope<char>("def abcd".ToRope(), "efgh".ToRope()).LastIndexOf("def".ToRope(), comparer));
        Assert.AreEqual("def abcdeFgh".LastIndexOf("fgh", StringComparison.OrdinalIgnoreCase), new Rope<char>("def abcd".ToRope(), "eFgh".ToRope()).LastIndexOf("fgh".ToRope(), comparer));
        Assert.AreEqual("abc abc".LastIndexOf("bc", 2, StringComparison.OrdinalIgnoreCase), ("ab".ToRope() + "c abc".ToRope()).LastIndexOf("bc".AsMemory(), 2, comparer));
    }

    [TestMethod]
    public void LastIndexOfRopeWithStartIndexIgnoreCaseEmptyString()
    {
        var comparer = CharComparer.OrdinalIgnoreCase;
#if NET8_0_OR_GREATER
        var expectedA = "ab".LastIndexOf(string.Empty, 1, StringComparison.OrdinalIgnoreCase);
        var expectedB = "ab".LastIndexOf(string.Empty, 2, StringComparison.OrdinalIgnoreCase);
#else
        // Apparently there was a breaking change for string handling between NET Core 3.0 and NET 5.0 - https://github.com/dotnet/runtime/issues/43736
        var expectedA = 2;
        var expectedB = 2;
#endif
        Assert.AreEqual(expectedA, new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf(Rope<char>.Empty, 1, comparer));
        Assert.AreEqual(expectedB, new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf(Rope<char>.Empty, 2, comparer));
    }

    [TestMethod]
    public void CommonPrefixLength()
    {
        var a = "I'm sorry Dave, I can't do that.".ToRope();
        var b = "I'm sorry Janine, I can't do that for you.".ToRope();
        Assert.AreEqual("I'm sorry ".Length, a.CommonPrefixLength(b));
    }

    [TestMethod]
    public void CommonPrefixLengthPartioned()
    {
        var a = "I'm".ToRope() + " sorry Dave, I can't do that.".ToRope();
        var b = "I'm sor".ToRope() + "ry Janine, I can't do that for you.".ToRope();
        Assert.AreEqual("I'm sorry ".Length, a.CommonPrefixLength(b));

        var ax = "I'm sorry".ToRope();
        var bx = "I".ToRope() + "'ve tried".ToRope();
        Assert.AreEqual("I'".Length, ax.CommonPrefixLength(bx));
    }

    [TestMethod]
    public void CommonPrefixLengthEqual()
    {
        var a = "I'm sorry Dave, I can't do that.".ToRope();
        var b = "I'm sorry Dave, I can't do that.".ToRope();
        Assert.AreEqual("I'm sorry Dave, I can't do that.".Length, a.CommonPrefixLength(b));
    }

    [TestMethod]
    public void CommonSuffixLength()
    {
        var a = "I'm sorry Dave, I can't do that".ToRope();
        var b = "I'm sorry Janine, I can't do that".ToRope();
        Assert.AreEqual("e, I can't do that".Length, a.CommonSuffixLength(b));
    }

    [TestMethod]
    public void CommonSuffixLengthPartitioned()
    {
        var a = "I'm sorry Dave, I can't ".ToRope() + "do that".ToRope();
        var b = "I'm sorry Janine, I can't do".ToRope() + " that".ToRope();
        Assert.AreEqual("e, I can't do that".Length, a.CommonSuffixLength(b));

        var ax = "Tests.".ToRope();
        var bx = "Vests".ToRope() + ".".ToRope();
        Assert.AreEqual("ests.".Length, ax.CommonSuffixLength(bx));
    }

    [TestMethod]
    public void CommonSuffixLengthEqual()
    {
        var a = "I'm sorry Dave, I can't do that.".ToRope();
        var b = "I'm sorry Dave, I can't do that.".ToRope();
        Assert.AreEqual("I'm sorry Dave, I can't do that.".Length, a.CommonSuffixLength(b));
    }

    [TestMethod]
    public void BinarySearchIntRopeLeftHalf() => Assert.AreEqual(5, TestData.EvenNumbers.BinarySearch(10));

    [TestMethod]
    public void BinarySearchIntRopeLeftHalfMissingGivesTwosComplementForInsertion() => Assert.AreEqual(6, ~TestData.EvenNumbers.BinarySearch(11));

    [TestMethod]
    public void BinarySearchIntRopeRightHalf() => Assert.AreEqual(1020 / 2, TestData.EvenNumbers.BinarySearch(1020));

    [TestMethod]
    public void BinarySearchIntRopeRightHalfMissingGivesTwosComplementForInsertion() => Assert.AreEqual((2021 / 2) + 1, ~TestData.EvenNumbers.BinarySearch(2021));

    [TestMethod]
    public void BinarySearchIntRopeLength() => Assert.AreEqual(1024, ~TestData.EvenNumbers.ToRope().BinarySearch(2048));

    [TestMethod]
    public void BinarySearchIntNegative() => Assert.AreEqual(-1, Enumerable.Range(0, 1024).ToRope().BinarySearch(-10));
}
