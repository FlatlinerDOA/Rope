namespace Rope.UnitTests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

[TestClass]
public sealed class RopeTests
{
    [TestMethod]
    public void LastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf('c', 2), "abc abc".ToRope().LastIndexOf("c".AsMemory(), 2));

	[TestMethod]
	public void ConcattedLastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf("bc", 2), ("ab".ToRope() + "c abc".ToRope()).LastIndexOf("bc".AsMemory(), 2));

	[TestMethod]
	public void IndexOf() => Assert.AreEqual("abc abc".IndexOf('c', 2), "abc abc".ToRope().IndexOf("c".AsMemory(), 2));

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
	public void ImplicitFromArray()
	{
		var chars = new[] { 'I', '\'', 'm', ' ', 's', 'o', 'r', 'r', 'y', ' ', 'D', 'a', 'v', 'e', '.' };
		Rope<char> r = chars;
		Assert.IsTrue(r.SequenceEqual(chars));
	}

	[TestMethod]
	public void CommonPrefixLength()
	{
		var a = "I'm sorry Dave, I can't do that".ToRope();
		var b = "I'm sorry Janine, I can't do that for you.".ToRope();
		Assert.AreEqual("I'm sorry ".Length, a.CommonPrefixLength(b));
	}

	[TestMethod]
	public void CommonSuffixLength()
	{
		var a = "I'm sorry Dave, I can't do that".ToRope();
		var b = "I'm sorry Janine, I can't do that".ToRope();
		Assert.AreEqual("e, I can't do that".Length, a.CommonSuffixLength(b));
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void ArgumentNullLeft() => new Rope<char>(null, Rope<char>.Empty);	

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void ArgumentNullRight() => new Rope<char>(Rope<char>.Empty, null);	

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

}