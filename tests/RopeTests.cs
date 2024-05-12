namespace Rope.UnitTests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[TestClass]
public sealed class RopeTests
{
    public RopeTests()
	{
        Trace.Listeners.Add(new ConsoleTraceListener());
	}

	[TestMethod]
	public void EndsWith() => Assert.IsTrue("n".ToRope().EndsWith("n".ToRope()));

	[TestMethod]
	public void EndsWithMemory() => Assert.IsTrue("testing".ToRope().EndsWith("ing".AsMemory()));

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
	public void NotStartsWithPartitioned() => Assert.IsFalse(new Rope<char>("cde".ToRope(), "f".ToRope()).StartsWith(new Rope<char>("cde".ToRope(), "g".ToRope())));

	[TestMethod]
	public void StartsWithMemory() => Assert.IsTrue("abcd".ToRope().StartsWith("ab".AsMemory()));

	[TestMethod]
	public void NotStartsWith() => Assert.IsFalse("dabcd".ToRope().StartsWith("ab".ToRope()));

	[TestMethod]
	public void ConvertingToString() => Assert.AreEqual("The ghosts say boo dee boo".ToRope().ToString(), "The ghosts say boo dee boo");

	[TestMethod]
	public void ReplaceElement()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();		
		Assert.AreEqual("I'm_sorry_Dave,_I_can't_do_that.", a.Replace(' ', '_').ToString());
	}

	[TestMethod]
	public void RemoveZeroLengthDoesNothing()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreEqual(a, a.RemoveRange(10, 0));
	}

	[TestMethod]
	public void RemoveAtTailDoesNothing()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreEqual(a, a.RemoveRange(a.Length));
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentOutOfRangeException))]
	public void RemoveBeyondTailArgumentOutOfRangeException()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreEqual(a, a.RemoveRange(a.Length + 1));
	}

	[TestMethod]
	[ExpectedException(typeof(IndexOutOfRangeException))]
    public void EmptyElementAtIndexOutOfRangeException() => Rope<char>.Empty.ElementAt(0);

	[TestMethod]
	[ExpectedException(typeof(IndexOutOfRangeException))]
    public void NodeElementAtIndexOutOfRangeException() => ("abc".ToRope() + "def".ToRope()).ElementAt(6);

	[TestMethod]
	[ExpectedException(typeof(IndexOutOfRangeException))]
    public void PartitionedElementAtIndexOutOfRangeException() => ("abc".ToRope() + "def".ToRope()).ElementAt(6);

//	[TestMethod]
//	[ExpectedException(typeof(ArgumentNullException))]
//#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
//    public void ArgumentNullLeft() => new Rope<char>((Rope<char>)(object)null, Rope<char>.Empty);
//#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

//    [TestMethod]
//	[ExpectedException(typeof(ArgumentNullException))]
//#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
//    public void ArgumentNullRight() => new Rope<char>(Rope<char>.Empty, (Rope<char>)(object)null);
//#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    [TestMethod]
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    public void NullNotEqualToEmptyRope() => Assert.IsFalse(object.Equals(null, Rope<char>.Empty));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    [TestMethod]
	public void StringEqualsOperator() => Assert.IsTrue("abc".ToRope() == "abc".ToRope());

	[TestMethod]
	public void StructuralEqualsOperator()
	{
        Assert.IsTrue(new Rope<char>('t') == new Rope<char>('t'));
        Assert.IsTrue(Rope<char>.Empty + "test".ToRope() == "t".ToRope() + "est".ToRope());
        Assert.IsTrue("t".ToRope() + "est".ToRope() == "t".ToRope() + "est".ToRope());
        Assert.IsTrue("te".ToRope() + "st".ToRope() == "t".ToRope() + "est".ToRope());
        Assert.IsTrue("tes".ToRope() + "t".ToRope() == "t".ToRope() + "est".ToRope());
        Assert.IsTrue("test".ToRope() + Rope<char>.Empty == "t".ToRope() + "est".ToRope());

        Assert.IsTrue("t".ToRope() + "est".ToRope() == Rope<char>.Empty + "test".ToRope());
        Assert.IsTrue("t".ToRope() + "est".ToRope() == "t".ToRope() + "est".ToRope());
        Assert.IsTrue("t".ToRope() + "est".ToRope() == "te".ToRope() + "st".ToRope());
        Assert.IsTrue("t".ToRope() + "est".ToRope() == "tes".ToRope() + "t".ToRope());
        Assert.IsTrue("t".ToRope() + "est".ToRope() == "test".ToRope() + Rope<char>.Empty);

    }

    [TestMethod]
	public void StringNotEqualsOperator() => Assert.IsTrue("abc".ToRope() != "abbc".ToRope());

	[TestMethod]
	public void StructuralNotEqualsOperator() => Assert.IsTrue("a".ToRope() + "bc".ToRope() != "ab".ToRope() + "bc".ToRope());

	[TestMethod]
	public void EmptyRopeNotEqualToNull() => Assert.IsFalse(Rope<char>.Empty.Equals((object)null));

	[TestMethod]
	public void EmptyRopeEqualsEmptyRope() => Assert.IsTrue(Rope<char>.Empty.Equals(Rope<char>.Empty));

	[TestMethod]
	public void CreateVeryLargeRopeFromArray() => Assert.IsTrue(Enumerable.Range(0, Rope<char>.MaxLeafLength * 4).Select(i => i.ToString()[0]).SequenceEqual(new Rope<char>(Enumerable.Range(0, Rope<char>.MaxLeafLength * 4).Select(i => i.ToString()[0]).ToArray())));

	[TestMethod]
	public void CreateVeryLargeRopeToRope() => Assert.IsTrue(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).SequenceEqual(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).ToRope()));

	[TestMethod]
	public void CreateVeryLargeRopeFromListToRope() => Assert.IsTrue(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).SequenceEqual(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).ToList().ToRope()));


	[TestMethod]
	public void InsertSortedEmpty() => Assert.IsTrue(Rope<int>.Empty.InsertSorted(1, Comparer<int>.Default).SequenceEqual(new[] { 1 }));

	[TestMethod]
	public void InsertSorted() => Assert.IsTrue(new[] { 0, 1, 3, 4, 5 }.ToRope().InsertSorted(2, Comparer<int>.Default).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }));

	[TestMethod]
	public void InsertSortedStart() => Assert.IsTrue(new[] { 0, 1, 2, 3, 4, 5 }.ToRope().InsertSorted(-1, Comparer<int>.Default).SequenceEqual(new[] { -1, 0, 1, 2, 3, 4, 5 }));

	[TestMethod]
	public void InsertSortedEnd() => Assert.IsTrue(new[] { 0, 1, 2, 3, 4, 5 }.ToRope().InsertSorted(6, Comparer<int>.Default).SequenceEqual(new[] { 0, 1, 2, 3, 4, 5, 6 }));

	[TestMethod]
	public void StructuralHashCodeEquivalence()
	{
        Assert.AreEqual("test".ToRope().GetHashCode(), (Rope<char>.Empty + "test".ToRope()).GetHashCode());
        Assert.AreEqual("test".ToRope().GetHashCode(), ("t".ToRope() + "est".ToRope()).GetHashCode());
        Assert.AreEqual("test".ToRope().GetHashCode(), ("te".ToRope() + "st".ToRope()).GetHashCode());
        Assert.AreEqual("test".ToRope().GetHashCode(), ("tes".ToRope() + "t".ToRope()).GetHashCode());
        Assert.AreEqual("test".ToRope().GetHashCode(), ("test".ToRope() + Rope<char>.Empty).GetHashCode());
    }

	[TestMethod]
	public void StructuralEqualsEquivalence()
	{
        Assert.AreEqual("test".ToRope(), Rope<char>.Empty + "test".ToRope());
        Assert.AreEqual("test".ToRope(), "t".ToRope() + "est".ToRope());
        Assert.AreEqual("test".ToRope(), "te".ToRope() + "st".ToRope());
        Assert.AreEqual("test".ToRope(), "tes".ToRope() + "t".ToRope());
        Assert.AreEqual("test".ToRope(), "test".ToRope() + Rope<char>.Empty);
    }

    [TestMethod]
	public void BalanceCheck()
	{
		var unbalancedRope = new Rope<char>(
				new Rope<char>(new char[] { 'a' }),
				new Rope<char>(
					new Rope<char>(new char[] { 'b' }),
					new Rope<char>(
						new Rope<char>(new char[] { 'c' }),
						new Rope<char>(
							new Rope<char>(new char[] { 'd' }),
							new Rope<char>(new Rope<char>(new char[] { 'e' }), new Rope<char>(new char[] { 'f' }))))));
		
		// Fn+2;
		Assert.IsFalse(unbalancedRope.IsBalanced);
		
		var balanced = unbalancedRope.Balanced();
		Assert.IsTrue(balanced.IsBalanced);
		
		// Flattened
		Assert.AreEqual(balanced.Depth, 0);
	}

	[TestMethod]
	[DataRow(1, 1)]
    [DataRow(2, 1)]
    [DataRow(2, 2)]
    [DataRow(3, 2)]
    [DataRow(5, 2)]
    [DataRow(256, 3)]
    [DataRow(256, 24)]
    public void ToString(int length, int chunkSize)
	{
		var (expected, rope) = TestData.Create(length, chunkSize);
		Assert.AreEqual(expected, rope.ToString());
	}
}