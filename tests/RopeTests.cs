namespace Rope.UnitTests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[TestClass]
public sealed class RopeTests
{
	private static readonly Rope<int> EvenNumbers = Enumerable.Range(0, 2048).Where(i => i % 2 == 0).ToRope();
	
	private static readonly Rope<char> LargeText = Enumerable.Range(0, 32 * 1024).Select(i => (char)(36 + i % 24)).ToRope();

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
    public void LastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf('c', 2), "abc abc".ToRope().LastIndexOf("c".AsMemory(), 2));

	[TestMethod]
	public void ConcattedLastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf("bc", 2), ("ab".ToRope() + "c abc".ToRope()).LastIndexOf("bc".AsMemory(), 2));

	[TestMethod]
	public void IndexOf() => Assert.AreEqual("abc abc".IndexOf('c', 2), "abc abc".ToRope().IndexOf("c".AsMemory(), 2));

	[TestMethod]
	public void IndexOfInOverlap() => Assert.AreEqual(4, ("abcdef".ToRope() + "ghijklm".ToRope()).IndexOf("efgh".ToRope()));

	[TestMethod]
	public void IndexOfInRight() => Assert.AreEqual(6, ("abcdef".ToRope() + "ghijklm".ToRope()).IndexOf("ghi".ToRope()));

	[TestMethod]
	public void IndexOfAfter() => Assert.AreEqual("abc abc".IndexOf('c', 3), "abc abc".ToRope().IndexOf('c', 3));

	[TestMethod]
	public void ConcattedIndexOf() => Assert.AreEqual("abc abc".IndexOf("bc", 2), ("ab".ToRope() + "c abc".ToRope()).IndexOf("bc".AsMemory(), 2));

	[TestMethod]
	public void EndsWith() => Assert.IsTrue("n".ToRope().EndsWith("n".ToRope()));

	[TestMethod]
	public void NotEndsWith() => Assert.IsFalse("ny ".ToRope().EndsWith("n".ToRope()));

	[TestMethod]
	public void ConcattedNotEndsWith() => Assert.IsFalse(("ny".ToRope() + " ".ToRope()).EndsWith("n".ToRope()));

	[TestMethod]
	public void HashCodesForTheSameStringMatch() => Assert.AreEqual("The girls sing".ToRope().GetHashCode(), "The girls sing".ToRope().GetHashCode());

	[TestMethod]
	public void HashCodesForTheConcatenatedStringMatch() => Assert.AreEqual(("The girls".ToRope() + " sing".ToRope()).GetHashCode(), "The girls sing".ToRope().GetHashCode());

	[TestMethod]
	public void StartsWith() => Assert.IsTrue("abcd".ToRope().StartsWith("ab".ToRope()));

	[TestMethod]
	public void NotStartsWith() => Assert.IsFalse("dabcd".ToRope().StartsWith("ab".ToRope()));

	[TestMethod]
	public void AdditionOperatorTwoRopes() => Assert.AreEqual("Lorem ipsum", ("Lorem ".ToRope() + "ipsum".ToRope()).ToString());

	[TestMethod]
	public void AdditionOperatorSingleElement() => Assert.AreEqual("Lorem ipsum", ("Lorem ipsu".ToRope() + 'm').ToString());

	[TestMethod]
	public void ConcatRopes() => Assert.AreEqual("Lorem ipsum", "Lorem ".ToRope().Concat("ipsum".ToRope()).ToString());

	[TestMethod]
	public void Replace() => Assert.AreEqual("The ghosts say boo dee boo", "The ghosts say doo dee doo".ToRope().Replace("doo".AsMemory(), "boo".AsMemory()).ToString());		

	[TestMethod]
	public void ConvertingToString() => Assert.AreEqual("The ghosts say boo dee boo".ToRope().ToString(), "The ghosts say boo dee boo");

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
	}

	[TestMethod]
	public void CommonSuffixLengthEqual()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		var b = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreEqual("I'm sorry Dave, I can't do that.".Length, a.CommonSuffixLength(b));
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public void ArgumentNullLeft() => new Rope<char>(null, Rope<char>.Empty);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    [TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public void ArgumentNullRight() => new Rope<char>(Rope<char>.Empty, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    [TestMethod]
	public void NullNotEqualToEmptyRope() => Assert.IsFalse(null == Rope<char>.Empty);

	[TestMethod]
	public void EmptyRopeNotEqualToNull() => Assert.IsFalse(Rope<char>.Empty.Equals(null));

	[TestMethod]
	public void EmptyRopeEqualsEmptyRope() => Assert.IsTrue(Rope<char>.Empty.Equals(Rope<char>.Empty));

	[TestMethod]
	public void EmptyRopeEqualsNewEmptyRope() => Assert.IsTrue(Rope<char>.Empty.Equals(new Rope<char>()));

	[TestMethod]
	public void CreateVeryLargeRope() => Assert.IsTrue(Enumerable.Range(0, Rope<char>.MaxLeafLength * 4).Select(i => i.ToString()[0]).SequenceEqual(new Rope<char>(Enumerable.Range(0, Rope<char>.MaxLeafLength * 4).Select(i => i.ToString()[0]).ToArray())));

	[TestMethod]
	public void BinarySearchIntRopeLeftHalf() => Assert.AreEqual(5, EvenNumbers.BinarySearch(10));

	[TestMethod]
	public void BinarySearchIntRopeLeftHalfMissingGivesTwosComplementForInsertion() => Assert.AreEqual(6, ~EvenNumbers.BinarySearch(11));

	[TestMethod]
	public void BinarySearchIntRopeRightHalf() => Assert.AreEqual(1020 / 2, EvenNumbers.BinarySearch(1020));

	[TestMethod]
	public void BinarySearchIntRopeRightHalfMissingGivesTwosComplementForInsertion() => Assert.AreEqual((2021 / 2) + 1, ~EvenNumbers.BinarySearch(2021));

	[TestMethod]
	public void BinarySearchIntRopeLength() => Assert.AreEqual(1024, ~EvenNumbers.ToRope().BinarySearch(2048));

	[TestMethod]
	public void BinarySearchIntNegative() => Assert.AreEqual(-1, Enumerable.Range(0, 1024).ToRope().BinarySearch(-10));

	[TestMethod]
	public void InsertSortedEmpty() => Assert.IsTrue(Rope<int>.Empty.InsertSorted(1, Comparer<int>.Default).SequenceEqual(new[] { 1 }));

	[TestMethod]
	public void InsertSorted() => Assert.IsTrue(new[] { 0, 1, 3, 4, 5 }.ToRope().InsertSorted(2, Comparer<int>.Default).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }));

	[TestMethod]
	public void InsertSortedStart() => Assert.IsTrue(new[] { 0, 1, 2, 3, 4, 5 }.ToRope().InsertSorted(-1, Comparer<int>.Default).SequenceEqual(new[] { -1, 0, 1, 2, 3, 4, 5 }));

	[TestMethod]
	public void InsertSortedEnd() => Assert.IsTrue(new[] { 0, 1, 2, 3, 4, 5 }.ToRope().InsertSorted(6, Comparer<int>.Default).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }));

	[TestMethod]
	public void StructuralHashCodeEquivalence() => Assert.AreEqual("test".ToRope().GetHashCode(), ("te".ToRope() + "st".ToRope()).GetHashCode());

	[TestMethod]
	public void StructuralEqualsEquivalence() => Assert.AreEqual("test".ToRope(), "te".ToRope() + "st".ToRope());

	[TestMethod]
	public void LargeAppend()
	{
		var s = new Rope<char>();
		for (int i = 0; i < 1000; i++)
		{
			s += LargeText;
		}

		Assert.AreEqual(s.Length,  LargeText.Length * 1000);
	}

	[TestMethod]
	public void InsertRope() 
	{
		var s = LargeText;
		for (int i = 0; i < 1000; i++)
		{
			s = s.Insert(LargeText.Length / 2, LargeText);
		}

		Assert.AreEqual(LargeText.Length * 1001, s.Length);
	}
	
	[TestMethod]
	public void InsertMemory() 
	{
		var memory = LargeText.ToMemory();
		var s = LargeText;
		for (int i = 0; i < 1000; i++)
		{
			s = s.Insert(memory.Length / 2, memory);
		}

		Assert.AreEqual(memory.Length * 1001, s.Length);
	}
}