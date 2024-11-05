namespace Rope.UnitTests;

using Rope.Compare;
using System.Collections.Immutable;
using System.Globalization;

[TestClass]
public class ImmutableListInterface
{
    public const string TestData = "ABC def gHiIİı JkL";

    public static readonly CultureInfo Turkish = CultureInfo.CreateSpecificCulture("tr-TR");

    public static IImmutableList<char> Subject = TestData.ToRope();

    private static readonly CharComparer TurkishCaseInsensitiveComparer = new CharComparer(
        Turkish,
        CompareOptions.IgnoreCase);

    [TestMethod]
    [DataRow('A', 0, 1)]
    [DataRow('a', 0, 1)]
    [DataRow('B', 0, 1)]
    [DataRow('b', 0, 1)]
    [DataRow('B', 1, 1)]
    [DataRow('b', 1, 1)]
    [DataRow('L', 0, 18)]
    [DataRow('l', 0, 18)]
    [DataRow('Z', 0, 18)]
    [DataRow('z', 0, 18)]
    [DataRow('I', 0, 18)]
    [DataRow('ı', 0, 18)]
    [DataRow('i', 0, 18)]
    [DataRow('İ', 0, 18)]
    [DataRow('A', 1, 1)]
    [DataRow('a', 1, 1)]
    [DataRow('B', 2, 1)]
    [DataRow('b', 2, 1)]
    [DataRow('L', 14, 4)]
    [DataRow('l', 14, 4)]
    [DataRow('Z', 14, 4)]
    [DataRow('z', 14, 4)]
    public void IndexOfOrdinal(char find, int startIndex, int count) => Assert.AreEqual(
        TestData.IndexOf(string.Empty + find, startIndex, count, StringComparison.Ordinal),
        Subject.IndexOf(find, startIndex, count, CharComparer.Ordinal));

    [TestMethod]
    [DataRow('A', 0, 1)]
    [DataRow('a', 0, 1)]
    [DataRow('B', 0, 1)]
    [DataRow('b', 0, 1)]
    [DataRow('B', 1, 1)]
    [DataRow('b', 1, 1)]
    [DataRow('L', 0, 18)]
    [DataRow('l', 0, 18)]
    [DataRow('Z', 0, 18)]
    [DataRow('z', 0, 18)]
#if NET8_0_OR_GREATER
    [DataRow('I', 0, 18)]
    [DataRow('ı', 0, 18)]
    [DataRow('i', 0, 18)]
    [DataRow('İ', 0, 18)]
#endif
    [DataRow('A', 1, 1)]
    [DataRow('a', 1, 1)]
    [DataRow('B', 2, 1)]
    [DataRow('b', 2, 1)]
    [DataRow('L', 14, 4)]
    [DataRow('l', 14, 4)]
    [DataRow('Z', 14, 4)]
    [DataRow('z', 14, 4)]
    public void IndexOfOrdinalIgnoreCase(char find, int startIndex, int count) => Assert.AreEqual(
        TestData.IndexOf(string.Empty + find, startIndex, count, StringComparison.OrdinalIgnoreCase),
        Subject.IndexOf(find, startIndex, count, CharComparer.OrdinalIgnoreCase));

    [TestMethod]
    [DataRow('A', 0, 1)]
    [DataRow('a', 0, 1)]
    [DataRow('B', 0, 1)]
    [DataRow('b', 0, 1)]
    [DataRow('B', 1, 1)]
    [DataRow('b', 1, 1)]
    [DataRow('L', 0, 18)]
    [DataRow('l', 0, 18)]
    [DataRow('Z', 0, 18)]
    [DataRow('z', 0, 18)]
#if NET8_0_OR_GREATER
    [DataRow('I', 0, 18)]
    [DataRow('ı', 0, 18)]
    [DataRow('i', 0, 18)]
    [DataRow('İ', 0, 18)]
#endif
    [DataRow('A', 1, 1)]
    [DataRow('a', 1, 1)]
    [DataRow('B', 2, 1)]
    [DataRow('b', 2, 1)]
    [DataRow('L', 14, 4)]
    [DataRow('l', 14, 4)]
    [DataRow('Z', 14, 4)]
    [DataRow('z', 14, 4)]
    public void IndexOfCultureIgnoreCase(char find, int startIndex, int count) => Assert.AreEqual(
        TestData.IndexOf(string.Empty + find, startIndex, count, StringComparison.InvariantCultureIgnoreCase),
        Subject.IndexOf(find, startIndex, count, CharComparer.InvariantCultureIgnoreCase));

    [TestMethod]
    [DataRow('A', 0, 1, 0)]
    [DataRow('a', 0, 1, 0)]
    [DataRow('B', 0, 2, 1)]
    [DataRow('b', 0, 2, 1)]
    [DataRow('B', 2, 1, -1)]
    [DataRow('b', 2, 1, -1)]
    [DataRow('L', 0, 18, 17)]
    [DataRow('l', 0, 18, 17)]
    [DataRow('Z', 0, 18, -1)]
    [DataRow('z', 0, 18, -1)]
#if NET8_0_OR_GREATER
    [DataRow('I', 0, 18, 11)]
    [DataRow('ı', 0, 18, 11)]
    [DataRow('i', 0, 18, 10)]
    [DataRow('İ', 0, 18, 10)]
#endif
    [DataRow('A', 1, 1, -1)]
    [DataRow('a', 1, 1, -1)]
    [DataRow('B', 2, 1, -1)]
    [DataRow('b', 2, 1, -1)]
    [DataRow('L', 18, 0, -1)]
    [DataRow('l', 18, 0, -1)]
    [DataRow('Z', 18, 0, -1)]
    [DataRow('z', 18, 0, -1)]
    public void IndexOfTurkishIgnoreCase(char find, int startIndex, int count, int expected) => Assert.AreEqual(      
      expected,
      Subject.IndexOf(find, startIndex, count, TurkishCaseInsensitiveComparer));


    [TestMethod]
    [DataRow('A', 0, 1)]
    [DataRow('a', 0, 1)]
    [DataRow('B', 0, 1)]
    [DataRow('b', 0, 1)]
    [DataRow('B', 1, 1)]
    [DataRow('b', 1, 1)]
    [DataRow('L', 18, 17)]
    [DataRow('l', 18, 17)]
    [DataRow('Z', 18, 17)]
    [DataRow('z', 18, 17)]
#if NET8_0_OR_GREATER
    [DataRow('I', 18, 17)]
    [DataRow('ı', 18, 17)]
    [DataRow('i', 18, 17)]
    [DataRow('İ', 18, 17)]
#endif
    [DataRow('A', 1, 1)]
    [DataRow('a', 1, 1)]
    [DataRow('B', 2, 1)]
    [DataRow('b', 2, 1)]
    [DataRow('L', 14, 4)]
    [DataRow('l', 14, 4)]
    [DataRow('Z', 14, 4)]
    [DataRow('z', 14, 4)]
    public void LastIndexOfOrdinal(char find, int startIndex, int count) => Assert.AreEqual(
      TestData.LastIndexOf(string.Empty + find, startIndex, count, StringComparison.Ordinal),
      Subject.LastIndexOf(find, startIndex, count, CharComparer.Ordinal));

    [TestMethod]
    [DataRow('A', 0, 1)]
    [DataRow('a', 0, 1)]
    [DataRow('B', 0, 1)]
    [DataRow('b', 0, 1)]
    [DataRow('B', 1, 1)]
    [DataRow('b', 1, 1)]
    [DataRow('L', 18, 17)]
    [DataRow('l', 18, 17)]
    [DataRow('Z', 18, 17)]
    [DataRow('z', 18, 17)]
#if NET8_0_OR_GREATER
    [DataRow('I', 18, 17)]
    [DataRow('ı', 18, 17)]
    [DataRow('i', 18, 17)]
    [DataRow('İ', 18, 17)]
#endif
    [DataRow('A', 1, 1)]
    [DataRow('a', 1, 1)]
    [DataRow('B', 2, 1)]
    [DataRow('b', 2, 1)]
    [DataRow('L', 14, 4)]
    [DataRow('l', 14, 4)]
    [DataRow('Z', 14, 4)]
    [DataRow('z', 14, 4)]
    public void LastIndexOfOrdinalIgnoreCase(char find, int startIndex, int count) => Assert.AreEqual(
        TestData.LastIndexOf(string.Empty + find, startIndex, count, StringComparison.OrdinalIgnoreCase),
        Subject.LastIndexOf(find, startIndex, count, CharComparer.OrdinalIgnoreCase));

    [TestMethod]
    [DataRow('A', 0, 1)]
    [DataRow('a', 0, 1)]
    [DataRow('B', 0, 1)]
    [DataRow('b', 0, 1)]
    [DataRow('B', 1, 1)]
    [DataRow('b', 1, 1)]
    [DataRow('L', 18, 17)]
    [DataRow('l', 18, 17)]
    [DataRow('Z', 18, 17)]
    [DataRow('z', 18, 17)]
#if NET8_0_OR_GREATER
    [DataRow('I', 18, 17)]
    [DataRow('ı', 18, 17)]
    [DataRow('i', 18, 17)]
    [DataRow('İ', 18, 17)]
#endif
    [DataRow('A', 1, 1)]
    [DataRow('a', 1, 1)]
    [DataRow('B', 2, 1)]
    [DataRow('b', 2, 1)]
    [DataRow('L', 14, 4)]
    [DataRow('l', 14, 4)]
    [DataRow('Z', 14, 4)]
    [DataRow('z', 14, 4)]
    public void LastIndexOfCultureIgnoreCase(char find, int startIndex, int count) => Assert.AreEqual(
        TestData.LastIndexOf(string.Empty + find, startIndex, count, StringComparison.InvariantCultureIgnoreCase),
        Subject.LastIndexOf(find, startIndex, count, CharComparer.InvariantCultureIgnoreCase));

    [TestMethod]
    [DataRow('A', 0, 1, 0)]
    [DataRow('a', 0, 1, 0)]
    [DataRow('B', 0, 1, -1)]
    [DataRow('b', 0, 1, -1)]
    [DataRow('B', 1, 1, 1)]
    [DataRow('b', 1, 1, 1)]
    [DataRow('L', 18, 17, 17)]
    [DataRow('l', 18, 17, 17)]
    [DataRow('Z', 18, 17, -1)]
    [DataRow('z', 18, 17, -1)]
#if NET8_0_OR_GREATER
    [DataRow('I', 18, 17, 13)]
    [DataRow('ı', 18, 17, 13)]
    [DataRow('i', 18, 17, 12)]
    [DataRow('İ', 18, 17, 12)]
#endif
    [DataRow('A', 1, 1, -1)]
    [DataRow('a', 1, 1, -1)]
    [DataRow('B', 2, 1, -1)]
    [DataRow('b', 2, 1, -1)]
    [DataRow('L', 14, 4, -1)]
    [DataRow('l', 14, 4, -1)]
    [DataRow('Z', 14, 4, -1)]
    [DataRow('z', 14, 4, -1)]
    public void LastIndexOfTurkishIgnoreCase(char find, int startIndex, int count, int expected) => Assert.AreEqual(
      expected,
      Subject.LastIndexOf(find, startIndex, count, TurkishCaseInsensitiveComparer));
}
