namespace Rope.UnitTests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[TestClass]
public class RopeTests
{
    [TestMethod]
    public void LastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf('c', 2), "abc abc".ToRope().LastIndexOf("c".AsMemory(), 2));

	[TestMethod]
	public void ConcattedLastIndexOf() => Assert.AreEqual("abc abc".LastIndexOf("bc", 2), ("ab".ToRope() + "c abc".ToRope()).LastIndexOf("bc".AsMemory(), 2));

	[TestMethod]
	public void IndexOf() => Assert.AreEqual("abc abc".IndexOf('c', 2), "abc abc".ToRope().IndexOf("c".AsMemory(), 2));

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
	public void AdditionOperator() => Assert.AreEqual("The girls sing", ("The ".ToRope() + "girls sing".ToRope()).ToString());

	[TestMethod]
	public void Concat() => Assert.AreEqual("The girls sing", "The ".ToRope().Concat("girls sing".ToRope()).ToString());

	[TestMethod]
	public void Replace() => Assert.AreEqual("The ghosts say boo dee boo", "The ghosts say doo dee doo".ToRope().Replace("doo".AsMemory(), "boo".AsMemory()).ToString());		
}