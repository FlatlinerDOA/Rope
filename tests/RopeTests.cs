namespace Rope.UnitTests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[TestClass]
public sealed class RopeTests
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private static Rope<int> EvenNumbers;

    private static Rope<char> LargeText;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	public RopeTests()
	{
        Trace.Listeners.Add(new ConsoleTraceListener());
	}

	[ClassInitialize]	  
  	public static void OneTimeSetup(TestContext context)
  	{
		EvenNumbers = Enumerable.Range(0, 2048).Where(i => i % 2 == 0).ToRope();
		LargeText = Enumerable.Range(0, 32 * 1024).Select(i => (char)(36 + i % 24)).ToRope();
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
		Assert.AreEqual("ab".LastIndexOf(string.Empty, 1), new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf(Rope<char>.Empty, 1));
		Assert.AreEqual("ab".LastIndexOf(string.Empty, 2), new Rope<char>("a".ToRope(), "b".ToRope()).LastIndexOf(Rope<char>.Empty, 2));
		Assert.AreEqual("def abcdefgh".LastIndexOf("def"), new Rope<char>("def abcd".ToRope(), "efgh".ToRope()).LastIndexOf("def".ToRope()));
	}

    [TestMethod]
    public void LastIndexOfElementWithStartIndex() => Assert.AreEqual("abc abc".LastIndexOf('c', 2), "abc abc".ToRope().LastIndexOf('c', 2));

    [TestMethod]
    public void LastIndexOfElement()
	{
 		Assert.AreEqual("abc abc".LastIndexOf('c'), "abc abc".ToRope().LastIndexOf('c'));
		Assert.AreEqual("abc abc".LastIndexOf('a'), new Rope<char>("abc a".ToRope(), "bc".ToRope()).LastIndexOf('a'));
		Assert.AreEqual("abc abc".LastIndexOf('a'), new Rope<char>("abc ".ToRope(), "abc".ToRope()).LastIndexOf('a'));
		Assert.AreEqual("abc abc".LastIndexOf('x'), new Rope<char>("abc ".ToRope(), "abc".ToRope()).LastIndexOf('x'));
	}

	[TestMethod]
	public void ConcattedLastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf("bc", 2), ("ab".ToRope() + "c abc".ToRope()).LastIndexOf("bc".AsMemory(), 2));

	[TestMethod]
	public void IndexOf()
	{
		Assert.AreEqual("abc abc".IndexOf('c', 2), "abc abc".ToRope().IndexOf("c".AsMemory(), 2));
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
		Assert.AreEqual("def abcdefgh".IndexOf("def"), new Rope<char>("def abcd".ToRope(), "efgh".ToRope()).IndexOf("def".ToRope()));

		Assert.AreEqual(0, "test".ToRope().IndexOf(new Rope<char>("test".ToRope(), Rope<char>.Empty)));
		Assert.AreEqual(0, new Rope<char>("test".ToRope(), Rope<char>.Empty).IndexOf("test".ToRope()));


		Assert.AreEqual(0, "test".ToRope().IndexOf(new Rope<char>("te".ToRope(), "st".ToRope())));
		Assert.AreEqual(0, new Rope<char>("tes".ToRope(), "t".ToRope()).IndexOf("test".ToRope()));

	}

	[TestMethod]
	public void IndexOfRopeAfter() => Assert.AreEqual("abc abc".IndexOf("bc", 2), "abc abc".ToRope().IndexOf("bc".ToRope(), 2));

	[TestMethod]
	public void IndexOfInOverlap() => Assert.AreEqual(4, ("abcdef".ToRope() + "ghijklm".ToRope()).IndexOf("efgh".ToRope()));

	[TestMethod]
	public void IndexOfInRight() => Assert.AreEqual(6, ("abcdef".ToRope() + "ghijklm".ToRope()).IndexOf("ghi".ToRope()));

	[TestMethod]
	public void IndexOfAfter() => Assert.AreEqual("abc abc".IndexOf('c', 3), "abc abc".ToRope().IndexOf('c', 3));

	[TestMethod]
	public void ConcattedIndexOf() => Assert.AreEqual("abc def".IndexOf('d'), ("ab".ToRope() + "c def".ToRope()).IndexOf('d'));

	[TestMethod]
	public void ConcattedIndexOfMemory() => Assert.AreEqual("abc abc".IndexOf("bc "), ("ab".ToRope() + "c abc".ToRope()).IndexOf("bc ".AsMemory()));

	[TestMethod]
	public void ConcattedIndexOfAfter() => Assert.AreEqual("abc abc".IndexOf("bc ", 1), ("ab".ToRope() + "c abc".ToRope()).IndexOf("bc ".AsMemory(), 1));

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
	public void NotStartsWithPartitioned() => Assert.IsFalse(new Rope<char>("cde".ToRope(), "f".ToRope()).StartsWith(new Rope<char>("cdf".ToRope(), "g".ToRope())));

	[TestMethod]
	public void StartsWithMemory() => Assert.IsTrue("abcd".ToRope().StartsWith("ab".AsMemory()));

	[TestMethod]
	public void NotStartsWith() => Assert.IsFalse("dabcd".ToRope().StartsWith("ab".ToRope()));

	[TestMethod]
	public void AdditionOperatorTwoRopes() => Assert.AreEqual("Lorem ipsum", ("Lorem ".ToRope() + "ipsum".ToRope()).ToString());

	[TestMethod]
	public void AdditionOperatorSingleElement() => Assert.AreEqual("Lorem ipsum", ("Lorem ipsu".ToRope() + 'm').ToString());

	[TestMethod]
	public void ConcatRopes() => Assert.AreEqual("Lorem ipsum", "Lorem ".ToRope().AddRange("ipsum".ToRope()).ToString());

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
	public void ReplaceElement()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();		
		Assert.AreEqual("I'm_sorry_Dave,_I_can't_do_that.", a.Replace(' ', '_').ToString());
	}

	[TestMethod]
	public void RemoveZeroLengthDoesNothing()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreSame(a, a.RemoveRange(10, 0));
	}

	[TestMethod]
	public void RemoveAtTailDoesNothing()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreSame(a, a.RemoveRange(a.Length));
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentOutOfRangeException))]
	public void RemoveBeyondTailArgumentOutOfRangeException()
	{
		var a = "I'm sorry Dave, I can't do that.".ToRope();
		Assert.AreSame(a, a.RemoveRange(a.Length + 1));
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
	public void StringEqualsOperator() => Assert.IsTrue("abc".ToRope() == "abc".ToRope());

	[TestMethod]
	public void StructuralEqualsOperator() => Assert.IsTrue("a".ToRope() + "bc".ToRope() == "ab".ToRope() + "c".ToRope());

	[TestMethod]
	public void StringNotEqualsOperator() => Assert.IsTrue("abc".ToRope() != "abbc".ToRope());

	[TestMethod]
	public void StructuralNotEqualsOperator() => Assert.IsTrue("a".ToRope() + "bc".ToRope() != "ab".ToRope() + "bc".ToRope());

	[TestMethod]
	public void EmptyRopeNotEqualToNull() => Assert.IsFalse(Rope<char>.Empty.Equals(null));

	[TestMethod]
	public void EmptyRopeEqualsEmptyRope() => Assert.IsTrue(Rope<char>.Empty.Equals(Rope<char>.Empty));

	[TestMethod]
	public void CreateVeryLargeRopeFromArray() => Assert.IsTrue(Enumerable.Range(0, Rope<char>.MaxLeafLength * 4).Select(i => i.ToString()[0]).SequenceEqual(new Rope<char>(Enumerable.Range(0, Rope<char>.MaxLeafLength * 4).Select(i => i.ToString()[0]).ToArray())));

	[TestMethod]
	public void CreateVeryLargeRopeToRope() => Assert.IsTrue(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).SequenceEqual(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).ToRope()));

	[TestMethod]
	public void CreateVeryLargeRopeFromListToRope() => Assert.IsTrue(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).SequenceEqual(Enumerable.Range(0, Rope<int>.MaxLeafLength * 40).ToList().ToRope()));

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
	public void CombineRopeOfRopes()
	{
		var ropeOfRopes = new[] { "te".ToRope(), "st".ToRope() }.ToRope();
		var actual = ropeOfRopes.Combine();
		Assert.AreEqual("test".ToRope(), actual);
	}

	[TestMethod]
	public void ConstructWithRightEmpty()
	{
		var actual = new Rope<char>("test".ToRope(), Rope<char>.Empty);
		Assert.IsFalse(actual.IsNode);
		Assert.AreEqual("test".ToRope(), actual);
	}

	[TestMethod]
	public void ConstructWithRightEmptyLeftNode()
	{
		var actual = new Rope<char>("te".ToRope() + "st".ToRope(), Rope<char>.Empty);
		Assert.IsTrue(actual.IsNode);
		Assert.AreEqual("test".ToRope(), actual);
	}

	[TestMethod]
	public void ConstructWithLeftEmpty()
	{
		var actual = new Rope<char>(Rope<char>.Empty, "test".ToRope());
		Assert.IsFalse(actual.IsNode);
		Assert.AreEqual("test".ToRope(), actual);
	}

	[TestMethod]
	public void ConstructWithLeftEmptyRightNode()
	{
		var actual = new Rope<char>(Rope<char>.Empty, "te".ToRope() + "st".ToRope());
		Assert.IsTrue(actual.IsNode);
		Assert.AreEqual("test".ToRope(), actual);
	}

	[TestMethod]
	public void LargeAppend()
	{
		var s = Rope<char>.Empty;
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
			s = s.InsertRange(LargeText.Length / 2, LargeText);
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
		foreach (var rank in Enumerable.Range(0, 65000).Select(s => random.NextSingle()))
		{
			rope = rope.InsertSorted(rank, comparer);
		}		
	}

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
	public void AblationOfSplitAt()
	{
		var sequence = Enumerable.Range(0, 1200).Select(c => (char)((int)'9' + (c % 26))).ToList();
		for (int chunkSize = 1; chunkSize < 100; chunkSize += 1)
		{
			var rope = sequence.Chunk(chunkSize).Select(chunk => new Rope<char>(chunk.ToArray())).Aggregate(Rope<char>.Empty, (prev, next) => prev + next);
			for	(int splitPoint = 0; splitPoint < rope.Length; splitPoint++)
			{
				var (left, right) = rope.SplitAt(splitPoint);
				Assert.AreEqual(splitPoint, left.Length);
				Assert.AreEqual(rope.Length - splitPoint, right.Length);
			}
		}
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
}