namespace Rope.UnitTests.IO;

using Rope.IO;

[TestClass]
public class BloomFilterTests
{
    [TestMethod]
    public void StartsWithOperation()
    {
        var b = new BloomFilter(1024, 3, SupportedOperationFlags.StartsWith);
        b.Add("ABC");
        Assert.IsTrue(b.MightStartWith("A"));
        Assert.IsTrue(b.MightStartWith("AB"));
        Assert.IsTrue(b.MightStartWith("ABC"));

        Assert.IsFalse(b.MightStartWith("BC"));
        Assert.IsFalse(b.MightStartWith("ABD"));
        Assert.IsFalse(b.MightStartWith("AX"));
        Assert.IsFalse(b.MightStartWith("Z"));
    }

    [TestMethod]
    public void ContainsOperation()
    {
        var b = new BloomFilter(1024, 3, SupportedOperationFlags.Contains);
        b.Add("ABCX");
        Assert.IsTrue(b.MightContain("A"));
        Assert.IsTrue(b.MightContain("B"));
        Assert.IsTrue(b.MightContain("C"));
        Assert.IsTrue(b.MightContain("X"));
        Assert.IsTrue(b.MightContain("ABCX"));
    }

    [TestMethod]
    public void ContainsOperationImplicitlySupportsAllOtherOperations()
    {
         var b = new BloomFilter(1024, 3, SupportedOperationFlags.Contains);
        b.Add("ABCX");

        Assert.IsTrue(b.MightStartWith("ABC"));
        Assert.IsTrue(b.MightEqual("ABCX"));
        Assert.IsTrue(b.MightEndWith("BCX"));
    }

    [TestMethod]
    public void EndsWithOperation()
    {
        var b = new BloomFilter(1024, 3, SupportedOperationFlags.EndsWith);
        b.Add("ABCX");
        Assert.IsFalse(b.MightEndWith("A"));
        Assert.IsTrue(b.MightEndWith("ABCX"));
        Assert.IsTrue(b.MightEndWith("BCX"));
        Assert.IsTrue(b.MightEndWith("X"));
    }

    [TestMethod]
    public void EqualsOperation()
    {
        var b = new BloomFilter(1024, 10, SupportedOperationFlags.Equals);
        b.Add("ABCX");
        Assert.IsFalse(b.MightEqual("ABC"));
        Assert.IsTrue(b.MightEqual("ABCX"));
        Assert.IsFalse(b.MightEqual("X"));
    
        // Equals implies starts with.
        Assert.IsTrue(b.MightStartWith("A"));
    }
}