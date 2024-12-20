/*
* Diff Match and Patch -- Test Harness
* Copyright 2018 The diff-match-patch Authors.
* https://github.com/google/diff-match-patch
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

namespace Rope.UnitTests;

using Rope.Compare;
using System.Diagnostics;

[TestClass]
public class DiffMatchPatchTests
{
    public DiffMatchPatchTests()
    {
        this.DiffOptions = DiffOptions<char>.LineLevel;
        this.PatchOptions = PatchOptions.Default;
    }

    public DiffOptions<char> DiffOptions { get; set; }

    public PatchOptions PatchOptions { get; set; }

    [TestMethod]
    public void DiffCommonPrefixTest()
    {
        // Detect any common prefix.
        AssertEquals("diff_commonPrefix: Null case.", 0, "abc".ToRope().CommonPrefixLength("xyz"));

        AssertEquals("diff_commonPrefix: Non-null case.", 4, "1234abcdef".ToRope().CommonPrefixLength("1234xyz"));

        AssertEquals("diff_commonPrefix: Whole case.", 4, "1234".ToRope().CommonPrefixLength("1234xyz"));
    }

    [TestMethod]
    public void DiffCommonSuffixTest()
    {
        // Detect any common suffix.
        AssertEquals("diff_commonSuffix: Null case.", 0, "abc".ToRope().CommonSuffixLength("xyz"));

        AssertEquals("diff_commonSuffix: Non-null case.", 4, "abcdef1234".ToRope().CommonSuffixLength("xyz1234"));

        AssertEquals("diff_commonSuffix: Whole case.", 4, "1234".ToRope().CommonSuffixLength("xyz1234"));
    }

    [TestMethod]
    public void DiffCommonOverlapTest()
    {
        // Detect any suffix/prefix overlap.
        AssertEquals("diff_commonOverlap: Null case.", 0, "".ToRope().CommonOverlapLength("abcd"));

        AssertEquals("diff_commonOverlap: Whole case.", 3, "abc".ToRope().CommonOverlapLength("abcd"));

        AssertEquals("diff_commonOverlap: No overlap.", 0, "123456".ToRope().CommonOverlapLength("abcd"));

        AssertEquals("diff_commonOverlap: No overlap #2.", 0, "abcdef".ToRope().CommonOverlapLength("cdfg"));

        AssertEquals("diff_commonOverlap: No overlap #3.", 0, "cdfg".ToRope().CommonOverlapLength("abcdef"));

        AssertEquals("diff_commonOverlap: Overlap.", 3, "123456xxx".ToRope().CommonOverlapLength("xxxabcd"));

        // Some overly clever languages (C#) may treat ligatures as equal to their
        // component letters.  E.g. U+FB01 == 'fi'
        AssertEquals("diff_commonOverlap: Unicode.", 0, "fi".ToRope().CommonOverlapLength("\ufb01i"));
    }

    [TestMethod]
    public void DiffHalfMatchTest()
    {
        this.DiffOptions = this.DiffOptions with
        {
            TimeoutSeconds = 1f
        };

        AssertNull("diff_halfMatch: No match #1.", "1234567890".ToRope().DiffHalfMatch("abcdef", this.DiffOptions));

        AssertNull("diff_halfMatch: No match #2.", "12345".ToRope().DiffHalfMatch("23", this.DiffOptions));

        AssertEquals("diff_halfMatch: Single Match #1.", new HalfMatch<char>("12", "90", "a", "z", "345678"), "1234567890".ToRope().DiffHalfMatch("a345678z", this.DiffOptions));

        AssertEquals("diff_halfMatch: Single Match #2.", new HalfMatch<char>("a", "z", "12", "90", "345678"), "a345678z".ToRope().DiffHalfMatch("1234567890", this.DiffOptions));

        AssertEquals("diff_halfMatch: Single Match #3.", new HalfMatch<char>("abc", "z", "1234", "0", "56789"), "abc56789z".ToRope().DiffHalfMatch("1234567890", this.DiffOptions));

        AssertEquals("diff_halfMatch: Single Match #4.", new HalfMatch<char>("a", "xyz", "1", "7890", "23456"), "a23456xyz".ToRope().DiffHalfMatch("1234567890", this.DiffOptions));

        AssertEquals("diff_halfMatch: Multiple Matches #1.", new HalfMatch<char>("12123", "123121", "a", "z", "1234123451234"), "121231234123451234123121".ToRope().DiffHalfMatch("a1234123451234z", this.DiffOptions));

        AssertEquals("diff_halfMatch: Multiple Matches #2.", new HalfMatch<char>("", "-=-=-=-=-=", "x", "", "x-=-=-=-=-=-=-="), "x-=-=-=-=-=-=-=-=-=-=-=-=".ToRope().DiffHalfMatch("xx-=-=-=-=-=-=-=", this.DiffOptions));

        AssertEquals("diff_halfMatch: Multiple Matches #3.", new HalfMatch<char>("-=-=-=-=-=", "", "", "y", "-=-=-=-=-=-=-=y"), "-=-=-=-=-=-=-=-=-=-=-=-=y".ToRope().DiffHalfMatch("-=-=-=-=-=-=-=yy", this.DiffOptions));

        // Optimal diff would be -q+x=H-i+e=lloHe+Hu=llo-Hew+y not -qHillo+x=HelloHe-w+Hulloy
        AssertEquals("diff_halfMatch: Non-optimal halfmatch.", new HalfMatch<char>("qHillo", "w", "x", "Hulloy", "HelloHe"), "qHilloHelloHew".ToRope().DiffHalfMatch("xHelloHeHulloy", this.DiffOptions));

        this.DiffOptions = this.DiffOptions with
        {
            TimeoutSeconds = 0
        };
        AssertNull("diff_halfMatch: Optimal no halfmatch.", "qHilloHelloHew".ToRope().DiffHalfMatch("xHelloHeHulloy", this.DiffOptions));
    }

    [TestMethod]
    public void DiffLinesToCharsTest()
    {
        // Convert lines down to characters.
        Rope<Rope<char>> tmpVector = Rope<Rope<char>>.Empty;
        tmpVector += "".ToRope();
        tmpVector += "alpha\n".ToRope();
        tmpVector += "beta\n".ToRope();
        var result = "alpha\nbeta\nalpha\n".ToRope().DiffChunksToChars("beta\nalpha\nbeta\n", this.DiffOptions);
        AssertEquals("diff_linesToChars: Shared lines #1.", "\u0001\u0002\u0001", result.Item1);
        AssertEquals("diff_linesToChars: Shared lines #2.", "\u0002\u0001\u0002", result.Item2);
        AssertEquals("diff_linesToChars: Shared lines #3.", tmpVector, result.Item3);

        tmpVector = tmpVector.Clear();
        tmpVector += "".ToRope();
        tmpVector += "alpha\r\n".ToRope();
        tmpVector += "beta\r\n".ToRope();
        tmpVector += "\r\n".ToRope();
        result = "".ToRope().DiffChunksToChars("alpha\r\nbeta\r\n\r\n\r\n", this.DiffOptions);
        AssertEquals("diff_linesToChars: Empty string and blank lines #1.", "".ToRope(), result.Item1);
        AssertEquals("diff_linesToChars: Empty string and blank lines #2.", "\u0001\u0002\u0003\u0003".ToRope(), result.Item2);
        AssertEquals("diff_linesToChars: Empty string and blank lines #3.", tmpVector, result.Item3);

        tmpVector = tmpVector.Clear();
        tmpVector += "".ToRope();
        tmpVector += "a".ToRope();
        tmpVector += "b".ToRope();
        result = "a".ToRope().DiffChunksToChars("b".ToRope(), this.DiffOptions);
        AssertEquals("diff_linesToChars: No linebreaks #1.", "\u0001".ToRope(), result.Item1);
        AssertEquals("diff_linesToChars: No linebreaks #2.", "\u0002".ToRope(), result.Item2);
        AssertEquals("diff_linesToChars: No linebreaks #3.", tmpVector, result.Item3);

        // More than 256 to reveal any 8-bit limitations.
        int n = 300;
        tmpVector = tmpVector.Clear();
        var lines = Rope<char>.Empty;
        var chars = Rope<char>.Empty;
        for (int i = 1; i < n + 1; i++)
        {
            tmpVector = tmpVector.Add((i + "\n").ToRope());
            lines = lines.AddRange((i + "\n").ToRope());
            chars = chars.Add(Convert.ToChar(i));
        }

        AssertEquals("Test initialization fail #1.", n, tmpVector.Count);
        AssertEquals("Test initialization fail #2.", n, chars.Length);
        tmpVector = tmpVector.Insert(0, "".ToRope());
        result = lines.DiffChunksToChars("".ToRope(), this.DiffOptions);
        AssertEquals("diff_linesToChars: More than 256 #1.", chars, result.FirstEncoded);
        AssertEquals("diff_linesToChars: More than 256 #2.", "".ToRope(), result.SecondEncoded);
        AssertEquals("diff_linesToChars: More than 256 #3.", tmpVector, result.Lines);
    }

    [TestMethod]
    public void AccumulateChunksIntoCharsTest()
    {
        var expectedDictionary = new Dictionary<Rope<char>, int>();
        var (expectedChars, expectedLineArray) = "ABC\n123\nXYZ\nDO\nRAY\nMI\n".ToRope()
            .AccumulateChunksIntoChars(Rope<Rope<char>>.Empty, expectedDictionary, 3, DiffOptions<char>.LineLevel);

        var actualDictionary = new Dictionary<Rope<char>, int>();
        var (actualChars, actualLineArray) = "ABC\n123\nXYZ\nDO\nRAY\nMI\n".ToRope()
            .AccumulateChunksIntoCharsSplit(Rope<Rope<char>>.Empty, actualDictionary, 3, DiffOptions<char>.LineLevel);

        AssertEquals("Dictionary don't match", expectedDictionary, actualDictionary);
        AssertEquals("Chars don't match", expectedChars, actualChars);
        AssertEquals("LineArray don't match", expectedLineArray, actualLineArray);


        var expectedLineHash = new Dictionary<Rope<char>, int>();
        expectedLineArray = Rope<Rope<char>>.Empty.Add(Rope<char>.Empty);
        (expectedChars, expectedLineArray) = "alpha\nbeta\nalpha\n".ToRope().AccumulateChunksIntoChars(expectedLineArray, expectedLineHash, 1000, this.DiffOptions);
        (expectedChars, expectedLineArray) = "beta\nalpha\nbeta\n".ToRope().AccumulateChunksIntoChars(expectedLineArray, expectedLineHash, 1000, this.DiffOptions);


        var actualLineHash = new Dictionary<Rope<char>, int>();
        actualLineArray = Rope<Rope<char>>.Empty.Add(Rope<char>.Empty);
        (actualChars, actualLineArray) = "alpha\nbeta\nalpha\n".ToRope().AccumulateChunksIntoCharsSplit(actualLineArray, actualLineHash, 1000, this.DiffOptions);
        (actualChars, actualLineArray) = "beta\nalpha\nbeta\n".ToRope().AccumulateChunksIntoCharsSplit(actualLineArray, actualLineHash, 1000, this.DiffOptions);

        AssertEquals("Dictionary don't match", expectedLineHash, actualLineHash);
        AssertEquals("Chars don't match", expectedChars, actualChars);
        AssertEquals("LineArray don't match", expectedLineArray, actualLineArray);
    }

    [TestMethod]
    public void DiffCharsToLinesTest()
    {
        // First check that Diff equality works.
        AssertTrue("diff_charsToLines: Equality #1.", new Diff<char>(Operation.Equal, "a").Equals(new Diff<char>(Operation.Equal, "a")));

        AssertEquals("diff_charsToLines: Equality #2.", new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Equal, "a"));

        // Convert chars up to lines.
        var diffs = new Rope<Diff<char>>(new[]
        {
            new Diff<char>(Operation.Equal, "\u0001\u0002\u0001"),
            new Diff<char>(Operation.Insert, "\u0002\u0001\u0002")
        });

        Rope<Rope<char>> tmpVector = Rope<Rope<char>>.Empty;
        tmpVector = tmpVector.Add("".AsMemory());
        tmpVector = tmpVector.Add("alpha\n".AsMemory());
        tmpVector = tmpVector.Add("beta\n".AsMemory());

        diffs = diffs.ConvertCharsToChunks(tmpVector);

        AssertEquals(
            "diff_charsToLines: Shared lines.",
            new Rope<Diff<char>>(
                new[]
                {
                    new Diff<char>(Operation.Equal, "alpha\nbeta\nalpha\n"),
                    new Diff<char>(Operation.Insert, "beta\nalpha\nbeta\n")
                }),
            diffs);


        // More than 256 to reveal any 8-bit limitations.
        int n = 300;
        tmpVector = tmpVector.Clear();
        var lineList = Rope<char>.Empty;
        var charList = Rope<char>.Empty;
        for (int i = 1; i < n + 1; i++)
        {
            tmpVector = tmpVector.Add((i + "\n").AsMemory());
            lineList = lineList.AddRange((i + "\n").AsMemory());
            charList = charList.Add(Convert.ToChar(i));
        }

        AssertEquals("Test initialization fail #3.", n, tmpVector.Count);
        string lines = lineList.ToString();
        string chars = charList.ToString();
        AssertEquals("Test initialization fail #4.", n, chars.Length);
        tmpVector = tmpVector.Insert(0, "".AsMemory());
        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, chars) });
        diffs = diffs.ConvertCharsToChunks(tmpVector);
        AssertEquals("diff_charsToLines: More than 256.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, lines) }), diffs);

        // More than 65536 to verify any 16-bit limitation.
        lineList = Rope<char>.Empty;
        for (int i = 0; i < 66000; i++)
        {
            lineList = lineList.AddRange((i + "\n").AsMemory());
        }

        lineList = lineList.ToMemory();
        var result = lineList.DiffChunksToChars(Rope<char>.Empty, this.DiffOptions);
        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, result.FirstEncoded) });
        diffs = diffs.ConvertCharsToChunks(result.Lines);
        AssertEquals("diff_charsToLines: More than 65536.", lineList, diffs[0].Items);
    }

    [TestMethod]
    public void DiffCleanupMergeTest()
    {
        // Cleanup a messy diff.
        // Null case.
        var diffs = Rope<Diff<char>>.Empty;
        AssertEquals("diff_cleanupMerge: Null case.", Rope<Diff<char>>.Empty, diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "b"), new Diff<char>(Operation.Insert, "c") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: No change case.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "b"), new Diff<char>(Operation.Insert, "c") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Equal, "b"), new Diff<char>(Operation.Equal, "c") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Merge equalities.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "abc") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "a"), new Diff<char>(Operation.Delete, "b"), new Diff<char>(Operation.Delete, "c") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Merge deletions.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "abc") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, "a"), new Diff<char>(Operation.Insert, "b"), new Diff<char>(Operation.Insert, "c") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Merge insertions.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, "abc") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "a"), new Diff<char>(Operation.Insert, "b"), new Diff<char>(Operation.Delete, "c"), new Diff<char>(Operation.Insert, "d"), new Diff<char>(Operation.Equal, "e"), new Diff<char>(Operation.Equal, "f") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Merge interweave.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "ac"), new Diff<char>(Operation.Insert, "bd"), new Diff<char>(Operation.Equal, "ef") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "a"), new Diff<char>(Operation.Insert, "abc"), new Diff<char>(Operation.Delete, "dc") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Prefix and suffix detection.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "d"), new Diff<char>(Operation.Insert, "b"), new Diff<char>(Operation.Equal, "c") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "x"), new Diff<char>(Operation.Delete, "a"), new Diff<char>(Operation.Insert, "abc"), new Diff<char>(Operation.Delete, "dc"), new Diff<char>(Operation.Equal, "y") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Prefix and suffix detection with equalities.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "xa"), new Diff<char>(Operation.Delete, "d"), new Diff<char>(Operation.Insert, "b"), new Diff<char>(Operation.Equal, "cy") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Insert, "ba"), new Diff<char>(Operation.Equal, "c") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Slide edit left.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, "ab"), new Diff<char>(Operation.Equal, "ac") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "c"), new Diff<char>(Operation.Insert, "ab"), new Diff<char>(Operation.Equal, "a") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Slide edit right.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "ca"), new Diff<char>(Operation.Insert, "ba") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "b"), new Diff<char>(Operation.Equal, "c"), new Diff<char>(Operation.Delete, "ac"), new Diff<char>(Operation.Equal, "x") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Slide edit left recursive.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "abc"), new Diff<char>(Operation.Equal, "acx") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "x"), new Diff<char>(Operation.Delete, "ca"), new Diff<char>(Operation.Equal, "c"), new Diff<char>(Operation.Delete, "b"), new Diff<char>(Operation.Equal, "a") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Slide edit right recursive.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "xca"), new Diff<char>(Operation.Delete, "cba") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "b"), new Diff<char>(Operation.Insert, "ab"), new Diff<char>(Operation.Equal, "c") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Empty merge.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, "a"), new Diff<char>(Operation.Equal, "bc") }), diffs);

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, ""), new Diff<char>(Operation.Insert, "a"), new Diff<char>(Operation.Equal, "b") });
        diffs = diffs.DiffCleanupMerge();
        AssertEquals("diff_cleanupMerge: Empty equality.", new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, "a"), new Diff<char>(Operation.Equal, "b") }), diffs);
    }

    [TestMethod]
    public void DiffCleanupSemanticLosslessTest()
    {
        // Slide diffs to match logical boundaries.
        var diffs = Rope<Diff<char>>.Empty;
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Null case.", Rope<Diff<char>>.Empty, diffs);

        diffs = new Rope<Diff<char>>(new[]
        {
            new Diff<char>(Operation.Equal, "AAA\r\n\r\nBBB"),
            new Diff<char>(Operation.Insert, "\r\nDDD\r\n\r\nBBB"),
            new Diff<char>(Operation.Equal, "\r\nEEE")
        });

        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Blank lines.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "AAA\r\n\r\n"),
        new Diff<char>(Operation.Insert, "BBB\r\nDDD\r\n\r\n"),
        new Diff<char>(Operation.Equal, "BBB\r\nEEE")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "AAA\r\nBBB"),
        new Diff<char>(Operation.Insert, " DDD\r\nBBB"),
        new Diff<char>(Operation.Equal, " EEE")});
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Line boundaries.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "AAA\r\n"),
        new Diff<char>(Operation.Insert, "BBB DDD\r\n"),
        new Diff<char>(Operation.Equal, "BBB EEE")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The c"),
        new Diff<char>(Operation.Insert, "ow and the c"),
        new Diff<char>(Operation.Equal, "at.")});
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Word boundaries.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The "),
        new Diff<char>(Operation.Insert, "cow and the "),
        new Diff<char>(Operation.Equal, "cat.")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The-c"),
        new Diff<char>(Operation.Insert, "ow-and-the-c"),
        new Diff<char>(Operation.Equal, "at.")});
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Alphanumeric boundaries.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The-"),
        new Diff<char>(Operation.Insert, "cow-and-the-"),
        new Diff<char>(Operation.Equal, "cat.")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "a"),
        new Diff<char>(Operation.Delete, "a"),
        new Diff<char>(Operation.Equal, "ax")});
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Hitting the start.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "a"),
        new Diff<char>(Operation.Equal, "aax")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "xa"),
        new Diff<char>(Operation.Delete, "a"),
        new Diff<char>(Operation.Equal, "a")});
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Hitting the end.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "xaa"),
        new Diff<char>(Operation.Delete, "a")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The xxx. The "),
        new Diff<char>(Operation.Insert, "zzz. The "),
        new Diff<char>(Operation.Equal, "yyy.")});
        diffs = diffs.DiffCleanupSemanticLossless();
        AssertEquals("diff_cleanupSemanticLossless: Sentence boundaries.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The xxx."),
        new Diff<char>(Operation.Insert, " The zzz."),
        new Diff<char>(Operation.Equal, " The yyy.")}), diffs);
    }

    [TestMethod]
    public void DiffCleanupSemanticTest()
    {
        // Cleanup semantically trivial equalities.
        // Null case.
        var diffs = Rope<Diff<char>>.Empty;
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Null case.", Rope<Diff<char>>.Empty, diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "cd"),
        new Diff<char>(Operation.Equal, "12"),
        new Diff<char>(Operation.Delete, "e")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: No elimination #1.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "cd"),
        new Diff<char>(Operation.Equal, "12"),
        new Diff<char>(Operation.Delete, "e")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Insert, "ABC"),
        new Diff<char>(Operation.Equal, "1234"),
        new Diff<char>(Operation.Delete, "wxyz")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: No elimination #2.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Insert, "ABC"),
        new Diff<char>(Operation.Equal, "1234"),
        new Diff<char>(Operation.Delete, "wxyz")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "a"),
        new Diff<char>(Operation.Equal, "b"),
        new Diff<char>(Operation.Delete, "c")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Simple elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Insert, "b")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Equal, "cd"),
        new Diff<char>(Operation.Delete, "e"),
        new Diff<char>(Operation.Equal, "f"),
        new Diff<char>(Operation.Insert, "g")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Backpass elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abcdef"),
        new Diff<char>(Operation.Insert, "cdfg")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Insert, "1"),
        new Diff<char>(Operation.Equal, "A"),
        new Diff<char>(Operation.Delete, "B"),
        new Diff<char>(Operation.Insert, "2"),
        new Diff<char>(Operation.Equal, "_"),
        new Diff<char>(Operation.Insert, "1"),
        new Diff<char>(Operation.Equal, "A"),
        new Diff<char>(Operation.Delete, "B"),
        new Diff<char>(Operation.Insert, "2")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Multiple elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "AB_AB"),
        new Diff<char>(Operation.Insert, "1A2_1A2")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The c"),
        new Diff<char>(Operation.Delete, "ow and the c"),
        new Diff<char>(Operation.Equal, "at.")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Word boundaries.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "The "),
        new Diff<char>(Operation.Delete, "cow and the "),
        new Diff<char>(Operation.Equal, "cat.")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abcxx"),
        new Diff<char>(Operation.Insert, "xxdef")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: No overlap elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abcxx"),
        new Diff<char>(Operation.Insert, "xxdef")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abcxxx"),
        new Diff<char>(Operation.Insert, "xxxdef")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Overlap elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Equal, "xxx"),
        new Diff<char>(Operation.Insert, "def")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "xxxabc"),
        new Diff<char>(Operation.Insert, "defxxx")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Reverse overlap elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Insert, "def"),
        new Diff<char>(Operation.Equal, "xxx"),
        new Diff<char>(Operation.Delete, "abc")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abcd1212"),
        new Diff<char>(Operation.Insert, "1212efghi"),
        new Diff<char>(Operation.Equal, "----"),
        new Diff<char>(Operation.Delete, "A3"),
        new Diff<char>(Operation.Insert, "3BC")});
        diffs = diffs.DiffCleanupSemantic();
        AssertEquals("diff_cleanupSemantic: Two overlap eliminations.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abcd"),
        new Diff<char>(Operation.Equal, "1212"),
        new Diff<char>(Operation.Insert, "efghi"),
        new Diff<char>(Operation.Equal, "----"),
        new Diff<char>(Operation.Delete, "A"),
        new Diff<char>(Operation.Equal, "3"),
        new Diff<char>(Operation.Insert, "BC")}), diffs);
    }

    [TestMethod]
    public void DiffCleanupEfficiencyTest()
    {
        // Cleanup operationally trivial equalities.
        this.DiffOptions = this.DiffOptions with { EditCost = 4 };
        var diffs = Rope<Diff<char>>.Empty;
        diffs = diffs.DiffCleanupEfficiency(this.DiffOptions);
        AssertEquals("diff_cleanupEfficiency: Null case.", Rope<Diff<char>>.Empty, diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "12"),
        new Diff<char>(Operation.Equal, "wxyz"),
        new Diff<char>(Operation.Delete, "cd"),
        new Diff<char>(Operation.Insert, "34")});
        diffs = diffs.DiffCleanupEfficiency(this.DiffOptions);
        AssertEquals("diff_cleanupEfficiency: No elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "12"),
        new Diff<char>(Operation.Equal, "wxyz"),
        new Diff<char>(Operation.Delete, "cd"),
        new Diff<char>(Operation.Insert, "34")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "12"),
        new Diff<char>(Operation.Equal, "xyz"),
        new Diff<char>(Operation.Delete, "cd"),
        new Diff<char>(Operation.Insert, "34")});
        diffs = diffs.DiffCleanupEfficiency(this.DiffOptions);
        AssertEquals("diff_cleanupEfficiency: Four-edit elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abxyzcd"),
        new Diff<char>(Operation.Insert, "12xyz34")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Insert, "12"),
        new Diff<char>(Operation.Equal, "x"),
        new Diff<char>(Operation.Delete, "cd"),
        new Diff<char>(Operation.Insert, "34")});
        diffs = diffs.DiffCleanupEfficiency(this.DiffOptions);
        AssertEquals("diff_cleanupEfficiency: Three-edit elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "xcd"),
        new Diff<char>(Operation.Insert, "12x34")}), diffs);

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "12"),
        new Diff<char>(Operation.Equal, "xy"),
        new Diff<char>(Operation.Insert, "34"),
        new Diff<char>(Operation.Equal, "z"),
        new Diff<char>(Operation.Delete, "cd"),
        new Diff<char>(Operation.Insert, "56")});
        diffs = diffs.DiffCleanupEfficiency(this.DiffOptions);
        AssertEquals("diff_cleanupEfficiency: Backpass elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abxyzcd"),
        new Diff<char>(Operation.Insert, "12xy34z56")}), diffs);

        this.DiffOptions = this.DiffOptions with { EditCost = 5 };
        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "ab"),
        new Diff<char>(Operation.Insert, "12"),
        new Diff<char>(Operation.Equal, "wxyz"),
        new Diff<char>(Operation.Delete, "cd"),
        new Diff<char>(Operation.Insert, "34")});
        diffs = diffs.DiffCleanupEfficiency(this.DiffOptions);
        AssertEquals("diff_cleanupEfficiency: High cost elimination.", new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abwxyzcd"),
        new Diff<char>(Operation.Insert, "12wxyz34")}), diffs);

        this.DiffOptions = this.DiffOptions with { EditCost = 4 };
    }

    [TestMethod]
    public void DiffPrettyHtmlTest()
    {
        // Pretty print.
        var diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "a\n"),
        new Diff<char>(Operation.Delete, "<B>b</B>"),
        new Diff<char>(Operation.Insert, "c&d")});
        AssertEquals(
            "diff_prettyHtml:",
            "<span>a&para;<br></span><del style=\"background:#ffe6e6;\">&lt;B&gt;b&lt;/B&gt;</del><ins style=\"background:#e6ffe6;\">c&amp;d</ins>".ToRope(),
            diffs.ToHtmlReport());
    }

    [TestMethod]
    public void DiffTextTest()
    {
        // Compute the source and destination texts.
        var diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "jump"),
        new Diff<char>(Operation.Delete, "s"),
        new Diff<char>(Operation.Insert, "ed"),
        new Diff<char>(Operation.Equal, " over "),
        new Diff<char>(Operation.Delete, "the"),
        new Diff<char>(Operation.Insert, "a"),
        new Diff<char>(Operation.Equal, " lazy")});
        AssertEquals("diff_text1:", "jumps over the lazy".ToRope(), diffs.ToSource());
        AssertEquals("diff_text2:", "jumped over a lazy".ToRope(), diffs.ToTarget());
    }

    [TestMethod]
    public void DiffEncodeDecode()
    {
        var source = "A-Z a-z 0-9 - _ . ! ~ * ' ( ) ; / ? : @ & = + $ , # ".ToRope();
        var encoded = source.DiffEncode();
        var decoded = encoded.DiffDecode();
        AssertEquals("Should be round trippable", source.ToString(), decoded.ToString());
    }

    [TestMethod]
    public void DiffDeltaTest()
    {
        // Convert a diff into delta string.
        var diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "jump"),
        new Diff<char>(Operation.Delete, "s"),
        new Diff<char>(Operation.Insert, "ed"),
        new Diff<char>(Operation.Equal, " over "),
        new Diff<char>(Operation.Delete, "the"),
        new Diff<char>(Operation.Insert, "a"),
        new Diff<char>(Operation.Equal, " lazy"),
        new Diff<char>(Operation.Insert, "old dog")});
        var text1 = diffs.ToSource();
        AssertEquals("diff_text1: Base text.", "jumps over the lazy".ToRope(), text1);

        var delta = diffs.ToDelta();
        AssertEquals("diff_toDelta:", "=4\t-1\t+ed\t=6\t-3\t+a\t=5\t+old dog", delta.ToString());

        // Convert delta string into a diff.
        AssertEquals("diff_fromDelta: Normal.", diffs, Delta.Parse(delta, text1));

        // Generates error (19 < 20).
        try
        {
            _ = Delta.Parse(delta, text1 + "x");
            AssertFail("diff_fromDelta: Too long.");
        }
        catch (ArgumentException)
        {
            // Exception expected.
        }

        // Generates error (19 > 18).
        try
        {
            _ = Delta.Parse(delta, text1.Slice(1));
            AssertFail("diff_fromDelta: Too short.");
        }
        catch (ArgumentException)
        {
            // Exception expected.
        }

        // Generates error (%c3%xy invalid Unicode).
        try
        {
            _ = Delta.Parse("+%c3%xy", Rope<char>.Empty);
            AssertFail("diff_fromDelta: Invalid character.");
        }
        catch (ArgumentException)
        {
            // Exception expected.
        }

        // Test deltas with special characters.
        char zero = (char)0;
        char one = (char)1;
        char two = (char)2;
        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "\u0680 " + zero + " \t %"),
        new Diff<char>(Operation.Delete, "\u0681 " + one + " \n ^"),
        new Diff<char>(Operation.Insert, "\u0682 " + two + " \\ |")});
        text1 = diffs.ToSource();
        AssertEquals("diff_text1: Unicode text.", ("\u0680 " + zero + " \t %\u0681 " + one + " \n ^").ToRope(), text1);

        delta = diffs.ToDelta();
        // Uppercase, due to UrlEncoder now uses upper.
        AssertEquals("diff_toDelta: Unicode.", "=7\t-7\t+%DA%82 %02 %5C %7C".ToRope(), delta);

        AssertEquals("diff_fromDelta: Unicode.", diffs, delta.Parse(text1));

        // Verify pool of unchanged characters.
        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Insert, "A-Z a-z 0-9 - _ . ! ~ * ' ( ) ; / ? : @ & = + $ , # ")});
        var text2 = diffs.ToTarget();
        AssertEquals("diff_text2: Unchanged characters.", "A-Z a-z 0-9 - _ . ! ~ * \' ( ) ; / ? : @ & = + $ , # ".ToRope(), text2);

        delta = diffs.ToDelta();
        AssertEquals("diff_toDelta: Unchanged characters.", "+A-Z a-z 0-9 - _ . ! ~ * \' ( ) ; / ? : @ & = + $ , # ".ToRope(), delta);

        // Convert delta string into a diff.
        AssertEquals("diff_fromDelta: Unchanged characters.", diffs, Delta.Parse(delta, "".ToRope()));

        // 160 kb string.
        var a = "abcdefghij".ToRope();
        for (int i = 0; i < 14; i++)
        {
            a += a;
        }
        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, a) });
        delta = diffs.ToDelta();
        AssertEquals("diff_toDelta: 160kb string.", ("+" + a).ToRope(), delta);

        // Convert delta string into a diff.
        AssertEquals("diff_fromDelta: 160kb string.", diffs, Delta.Parse(delta, "".ToRope()));
    }

    [TestMethod]
    public void DiffXIndexTest()
    {
        // Translate a location in text1 to text2.
        var diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "a"),
        new Diff<char>(Operation.Insert, "1234"),
        new Diff<char>(Operation.Equal, "xyz")});
        AssertEquals("diff_xIndex: Translation on equality.", 5, (int)diffs.TranslateToTargetIndex(2));

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "a"),
        new Diff<char>(Operation.Delete, "1234"),
        new Diff<char>(Operation.Equal, "xyz")});
        AssertEquals("diff_xIndex: Translation on deletion.", 1, (int)diffs.TranslateToTargetIndex(3));
    }

    [TestMethod]
    public void CalculateEditDistanceTest()
    {
        var diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Insert, "1234"),
        new Diff<char>(Operation.Equal, "xyz")});
        AssertEquals("diff_levenshtein: Levenshtein with trailing equality.", 4, (int)diffs.CalculateEditDistance());

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Equal, "xyz"),
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Insert, "1234")});
        AssertEquals("diff_levenshtein: Levenshtein with leading equality.", 4, (int)diffs.CalculateEditDistance());

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "abc"),
        new Diff<char>(Operation.Equal, "xyz"),
        new Diff<char>(Operation.Insert, "1234")});
        AssertEquals("diff_levenshtein: Levenshtein with middle equality.", 7, (int)diffs.CalculateEditDistance());
    }

    [TestMethod]
    public void DiffBisectTest()
    {
        // Normal.
        var a = "cat".ToRope();
        var b = "map".ToRope();
        // Since the resulting diff hasn't been normalized, it would be ok if
        // the insertion and deletion pairs are swapped.
        // If the order changes, tweak this test as required.
        var diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "c"), new Diff<char>(Operation.Insert, "m"), new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "t"), new Diff<char>(Operation.Insert, "p") });
        AssertEquals("diff_bisect: Normal.", diffs, a.DiffBisect(b, this.DiffOptions, CancellationToken.None));

        // Timeout.
        var timedOut = new CancellationTokenSource();
        timedOut.Cancel();

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "cat"), new Diff<char>(Operation.Insert, "map") });
        AssertEquals("diff_bisect: Timeout.", diffs, a.DiffBisect(b, this.DiffOptions, timedOut.Token));
    }

    [TestMethod]
    public void CalculateDifferencesTest()
    {
        // Perform a trivial diff.
        var diffs = new Rope<Diff<char>>(Array.Empty<Diff<char>>());
        AssertEquals("diff_main: Null case.", diffs, "".Diff("", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "abc") });
        AssertEquals("diff_main: Equality.", diffs, "abc".Diff("abc", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "ab"), new Diff<char>(Operation.Insert, "123"), new Diff<char>(Operation.Equal, "c") });
        AssertEquals("diff_main: Simple insertion.", diffs, "abc".Diff("ab123c", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "123"), new Diff<char>(Operation.Equal, "bc") });
        AssertEquals("diff_main: Simple deletion.", diffs, "a123bc".Diff("abc", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Insert, "123"), new Diff<char>(Operation.Equal, "b"), new Diff<char>(Operation.Insert, "456"), new Diff<char>(Operation.Equal, "c") });
        AssertEquals("diff_main: Two insertions.", diffs, "abc".Diff("a123b456c", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "123"), new Diff<char>(Operation.Equal, "b"), new Diff<char>(Operation.Delete, "456"), new Diff<char>(Operation.Equal, "c") });
        AssertEquals("diff_main: Two deletions.", diffs, "a123b456c".Diff("abc", this.DiffOptions.WithChunking(false)));

        // Perform a real diff.
        // Switch off the timeout.
        this.DiffOptions = this.DiffOptions with { TimeoutSeconds = 0 };

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "a"), new Diff<char>(Operation.Insert, "b") });
        AssertEquals("diff_main: Simple case #1.", diffs, "a".Diff("b", false));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "Apple"), new Diff<char>(Operation.Insert, "Banana"), new Diff<char>(Operation.Equal, "s are a"), new Diff<char>(Operation.Insert, "lso"), new Diff<char>(Operation.Equal, " fruit.") });
        AssertEquals("diff_main: Simple case #2.", diffs, "Apples are a fruit.".Diff("Bananas are also fruit.", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "a"), new Diff<char>(Operation.Insert, "\u0680"), new Diff<char>(Operation.Equal, "x"), new Diff<char>(Operation.Delete, "\t"), new Diff<char>(Operation.Insert, new string(new char[] { (char)0 })) });
        AssertEquals("diff_main: Simple case #3.", diffs, "ax\t".Diff("\u0680x" + (char)0, this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "1"), new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "y"), new Diff<char>(Operation.Equal, "b"), new Diff<char>(Operation.Delete, "2"), new Diff<char>(Operation.Insert, "xab") });
        AssertEquals("diff_main: Overlap #1.", diffs, "1ayb2".Diff("abxab", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, "xaxcx"), new Diff<char>(Operation.Equal, "abc"), new Diff<char>(Operation.Delete, "y") });
        AssertEquals("diff_main: Overlap #2.", diffs, "abcy".Diff("xaxcxabc", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Delete, "ABCD"), new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Delete, "="), new Diff<char>(Operation.Insert, "-"), new Diff<char>(Operation.Equal, "bcd"), new Diff<char>(Operation.Delete, "="), new Diff<char>(Operation.Insert, "-"), new Diff<char>(Operation.Equal, "efghijklmnopqrs"), new Diff<char>(Operation.Delete, "EFGHIJKLMNOefg") });
        AssertEquals("diff_main: Overlap #3.", diffs, "ABCDa=bcd=efghijklmnopqrsEFGHIJKLMNOefg".Diff("a-bcd-efghijklmnopqrs", this.DiffOptions.WithChunking(false)));

        diffs = new Rope<Diff<char>>(new[] { new Diff<char>(Operation.Insert, " "), new Diff<char>(Operation.Equal, "a"), new Diff<char>(Operation.Insert, "nd"), new Diff<char>(Operation.Equal, " [[Pennsylvania]]"), new Diff<char>(Operation.Delete, " and [[New") });
        AssertEquals("diff_main: Large equality.", diffs, "a [[Pennsylvania]] and [[New".Diff(" and [[Pennsylvania]]", this.DiffOptions.WithChunking(false)));

        this.DiffOptions = this.DiffOptions with { TimeoutSeconds = 0.1f }; // 100ms
        string a = "`Twas brillig, and the slithy toves\nDid gyre and gimble in the wabe:\nAll mimsy were the borogoves,\nAnd the mome raths outgrabe.\n";
        string b = "I am the very model of a modern major general,\nI've information vegetable, animal, and mineral,\nI know the kings of England, and I quote the fights historical,\nFrom Marathon to Waterloo, in order categorical.\n";
        // Increase the text lengths by 1024 times to ensure a timeout.
        for (int i = 0; i < 10; i++)
        {
            a += a;
            b += b;
        }

        var s = Stopwatch.StartNew();
        _ = a.Diff(b, this.DiffOptions);
        s.Stop();

        // Test that we took at least the timeout period.
        // INCONCLUSIVE: AssertTrue("diff_main: Timeout min.", TimeSpan.FromSeconds(this.DiffOptions.TimeoutSeconds) <= s.Elapsed);
        // Test that we didn't take forever (be forgiving).
        // Theoretically this test could fail very occasionally if the
        // OS task swaps or locks up for a second at the wrong moment.
        // INCONCLUSIVE: AssertTrue("diff_main: Timeout max.", TimeSpan.FromSeconds(this.DiffOptions.TimeoutSeconds) * 2 > s.Elapsed);
        this.DiffOptions = this.DiffOptions with { TimeoutSeconds = 0 };

        // Test the linemode speedup.
        // Must be long to pass the 100 char cutoff.
        a = "1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n";
        b = "abcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\nabcdefghij\n";
        AssertEquals("diff_main: Simple line-mode.", a.Diff(b, this.DiffOptions.WithChunking(true)), a.Diff(b, this.DiffOptions.WithChunking(false)));

        a = "1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890";
        b = "abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghij";
        AssertEquals("diff_main: Single line-mode.", a.Diff(b, this.DiffOptions.WithChunking(true)), a.Diff(b, this.DiffOptions.WithChunking(false)));

        a = "1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n1234567890\n";
        b = "abcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n1234567890\n1234567890\n1234567890\nabcdefghij\n";
        var texts_linemode = a.Diff(b, this.DiffOptions.WithChunking(true)).ToSourceAndTarget();
        var texts_textmode = a.Diff(b, this.DiffOptions.WithChunking(false)).ToSourceAndTarget();
        AssertEquals("diff_main: Overlap line-mode. Source", texts_textmode.Source, texts_linemode.Source);
        AssertEquals("diff_main: Overlap line-mode. Destination", texts_textmode.Target, texts_linemode.Target);

        // Test null inputs -- not needed because nulls can't be passed in C#.
    }

    [TestMethod]
    public void MatchAlphabetTest()
    {
        {
            // Initialise the bitmasks for Bitap.
            var bitmask = new Dictionary<char, long>
            {
                { 'a', 4 },
                { 'b', 2 },
                { 'c', 1 }
            };
            AssertEquals("match_alphabet: Unique.", bitmask, "abc".ToRope().MatchAlphabet());
        }
        {
            var bitmask = new Dictionary<char, long>
            {
                { 'a', 37 },
                { 'b', 18 },
                { 'c', 8 }
            };

            AssertEquals("match_alphabet: Duplicates.", bitmask, "abcaba".ToRope().MatchAlphabet());
        }
    }

    [TestMethod]
    public void MatchBitapTest()
    {
        // Bitap algorithm.
        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.5f, MatchDistance = 100 };
        AssertEquals("match_bitap: Exact match #1.", 5, "abcdefghijk".ToRope().MatchBitap("fgh".ToRope(), 5, this.PatchOptions));

        AssertEquals("match_bitap: Exact match #2.", 5, "abcdefghijk".ToRope().MatchBitap("fgh".ToRope(), 0, this.PatchOptions));

        AssertEquals("MatchBitap: Fuzzy match #1.", 4, "abcdefghijk".ToRope().MatchBitap("efxhi".ToRope(), 0, this.PatchOptions));

        AssertEquals("MatchBitap: Fuzzy match #2.", 2, "abcdefghijk".ToRope().MatchBitap("cdefxyhijk".ToRope(), 5, this.PatchOptions));

        AssertEquals("MatchBitap: Fuzzy match #3.", -1, "abcdefghijk".ToRope().MatchBitap("bxy".ToRope(), 1, this.PatchOptions));

        AssertEquals("MatchBitap: Overflow.", 2, "123456789xx0".ToRope().MatchBitap("3456789x0".ToRope(), 2, this.PatchOptions));

        AssertEquals("MatchBitap: Before start match.", 0, "abcdef".ToRope().MatchBitap("xxabc".ToRope(), 4, this.PatchOptions));

        AssertEquals("MatchBitap: Beyond end match.", 3, "abcdef".ToRope().MatchBitap("defyy".ToRope(), 4, this.PatchOptions));

        AssertEquals("MatchBitap: Oversized pattern.", 0, "abcdef".ToRope().MatchBitap("xabcdefy".ToRope(), 0, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.4f };
        AssertEquals("MatchBitap: Threshold #1.", 4, "abcdefghijk".ToRope().MatchBitap("efxyhi".ToRope(), 1, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.3f };
        AssertEquals("MatchBitap: Threshold #2.", -1, "abcdefghijk".ToRope().MatchBitap("efxyhi".ToRope(), 1, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.0f };
        AssertEquals("MatchBitap: Threshold #3.", 1, "abcdefghijk".ToRope().MatchBitap("bcdef".ToRope(), 1, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.5f };
        AssertEquals("MatchBitap: Multiple select #1.", 0, "abcdexyzabcde".ToRope().MatchBitap("abccde".ToRope(), 3, this.PatchOptions));

        AssertEquals("MatchBitap: Multiple select #2.", 8, "abcdexyzabcde".ToRope().MatchBitap("abccde".ToRope(), 5, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchDistance = 10 };  // Strict location.
        AssertEquals("MatchBitap: Distance test #1.", -1, "abcdefghijklmnopqrstuvwxyz".ToRope().MatchBitap("abcdefg".ToRope(), 24, this.PatchOptions));

        AssertEquals("MatchBitap: Distance test #2.", 0, "abcdefghijklmnopqrstuvwxyz".ToRope().MatchBitap("abcdxxefg".ToRope(), 1, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchDistance = 1000 };  // Loose location.
        AssertEquals("MatchBitap: Distance test #3.", 0, "abcdefghijklmnopqrstuvwxyz".ToRope().MatchBitap("abcdefg".ToRope(), 24, this.PatchOptions));
    }

    [TestMethod]
    public void MatchMainTest()
    {
        // Full match.
        AssertEquals("match_main: Equality.", 0, "abcdef".MatchPattern("abcdef", 1000, this.PatchOptions));

        AssertEquals("match_main: Null text.", -1, "".MatchPattern("abcdef", 1, this.PatchOptions));

        AssertEquals("match_main: Null pattern.", 3, "abcdef".MatchPattern("", 3, this.PatchOptions));

        AssertEquals("match_main: Exact match.", 3, "abcdef".MatchPattern("de", 3, this.PatchOptions));

        AssertEquals("match_main: Beyond end match.", 3, "abcdef".MatchPattern("defy", 4, this.PatchOptions));

        AssertEquals("match_main: Oversized pattern.", 0, "abcdef".MatchPattern("abcdefy", 0, this.PatchOptions));

        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.7f };
        AssertEquals("match_main: Complex match.", 4, "I am the very model of a modern major general.".MatchPattern(" that berry ", 5, this.PatchOptions));
        this.PatchOptions = this.PatchOptions with { MatchThreshold = 0.5f };

        // Test null inputs -- not needed because nulls can't be passed in C#.
    }

    [TestMethod]
    public void PatchObjectTest()
    {
        // Patch Object.
        var p = new Patch<char>()
        {
            Start1 = 20,
            Start2 = 21,
            Length1 = 18,
            Length2 = 17,
            Diffs = Rope<Diff<char>>.Empty.AddRange(new[] {
            new Diff<char>(Operation.Equal, "jump"),
            new Diff<char>(Operation.Delete, "s"),
            new Diff<char>(Operation.Insert, "ed"),
            new Diff<char>(Operation.Equal, " over "),
            new Diff<char>(Operation.Delete, "the"),
            new Diff<char>(Operation.Insert, "a"),
            new Diff<char>(Operation.Equal, "\nlaz")})
        };
        string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n %0Alaz\n";
        AssertEquals("Patch: toString.", strp, p.ToString());
    }

    [TestMethod]
    public void PatchFromTextTest()
    {
        AssertTrue("patch_fromText: #0.", Patches.Parse("").Count == 0);

        string strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n %0Alaz\n";
        AssertEquals("patch_fromText: #1.", strp, Patches.Parse(strp.ToRope())[0].ToString());
        AssertEquals("patch_fromText: #2.", "@@ -1 +1 @@\n-a\n+b\n", Patches.Parse("@@ -1 +1 @@\n-a\n+b\n")[0].ToString());
        AssertEquals("patch_fromText: #3.", "@@ -1,3 +0,0 @@\n-abc\n", Patches.Parse("@@ -1,3 +0,0 @@\n-abc\n")[0].ToString());
        AssertEquals("patch_fromText: #4.", "@@ -0,0 +1,3 @@\n+abc\n", Patches.Parse("@@ -0,0 +1,3 @@\n+abc\n")[0].ToString());

        // Generates error.
        try
        {
            Patches.Parse("Bad\nPatch\n".ToRope());
            AssertFail("patch_fromText: #5.");
        }
        catch (ArgumentException)
        {
            // Exception expected.
        }
    }

    [TestMethod]
    public void PatchToTextTest()
    {
        var strp = "@@ -21,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n  laz\n".ToRope();
        var patches = Patches.Parse(strp);
        var result = patches.ToPatchString();
        AssertEquals("patch_toText: Single.", strp, result);

        strp = "@@ -1,9 +1,9 @@\n-f\n+F\n oo+fooba\n@@ -7,9 +7,9 @@\n obar\n-,\n+.\n  tes\n".ToRope();
        patches = Patches.Parse(strp);
        result = patches.ToPatchString();
        AssertEquals("patch_toText: Dual.", strp, result);
    }

    [TestMethod]
    public void PatchAddContextTest()
    {
        this.PatchOptions = this.PatchOptions with { Margin = 4 };

        Patch<char> p;
        p = Patches.Parse("@@ -21,4 +21,10 @@\n-jump\n+somersault\n")[0];
        p = p.PatchAddContext("The quick brown fox jumps over the lazy dog.", this.PatchOptions);
        AssertEquals("patch_addContext: Simple case.", "@@ -17,12 +17,18 @@\n fox \n-jump\n+somersault\n s ov\n", p.ToString());

        p = Patches.Parse("@@ -21,4 +21,10 @@\n-jump\n+somersault\n")[0];
        p = p.PatchAddContext("The quick brown fox jumps.", this.PatchOptions);
        AssertEquals("patch_addContext: Not enough trailing context.", "@@ -17,10 +17,16 @@\n fox \n-jump\n+somersault\n s.\n", p.ToString());

        p = Patches.Parse("@@ -3 +3,2 @@\n-e\n+at\n")[0];
        p = p.PatchAddContext("The quick brown fox jumps.", this.PatchOptions);
        AssertEquals("patch_addContext: Not enough leading context.", "@@ -1,7 +1,8 @@\n Th\n-e\n+at\n  qui\n", p.ToString());

        p = Patches.Parse("@@ -3 +3,2 @@\n-e\n+at\n".ToRope())[0];
        p = p.PatchAddContext("The quick brown fox jumps.  The quick brown fox crashes.", this.PatchOptions);
        AssertEquals("patch_addContext: Ambiguity.", "@@ -1,27 +1,28 @@\n Th\n-e\n+at\n  quick brown fox jumps. \n", p.ToString());
    }

    [TestMethod]
    public void PatchMakeTest()
    {
        Rope<Patch<char>> patches = Rope<Patch<char>>.Empty;
        patches = Patches.Create("", "");
        AssertEquals("patch_make: Null case.", "", patches.ToPatchString());

        string text1 = "The quick brown fox jumps over the lazy dog.";
        string text2 = "That quick brown fox jumped over a lazy dog.";
        string expectedPatch = "@@ -1,8 +1,7 @@\n Th\n-at\n+e\n  qui\n@@ -21,17 +21,18 @@\n jump\n-ed\n+s\n  over \n-a\n+the\n  laz\n";
        // The second patch must be "-21,17 +21,18", not "-22,17 +21,18" due to rolling context.
        patches = Patches.Create(text2, text1);
        var patchText = patches.ToPatchString();
        AssertEquals("patch_make: Text2+Text1 inputs.", expectedPatch, patchText);

        expectedPatch = "@@ -1,11 +1,12 @@\n Th\n-e\n+at\n  quick b\n@@ -22,18 +22,17 @@\n jump\n-s\n+ed\n  over \n-the\n+a\n  laz\n";
        patches = Patches.Create(text1, text2);
        AssertEquals("patch_make: Text1+Text2 inputs.", expectedPatch, patches.ToPatchString());

        var diffs = text1.Diff(text2, false);
        patches = diffs.ToPatches();
        AssertEquals("patch_make: Diff input.", expectedPatch, patches.ToPatchString());

        patches = diffs.ToPatches(text1, this.PatchOptions);
        AssertEquals("patch_make: Text1+Diff inputs.", expectedPatch, patches.ToPatchString());

        patches = Patches.Create("`1234567890-=[]\\;',./", "~!@#$%^&*()_+{}|:\"<>?");
        AssertEquals("patch_toText: Character encoding.",
            "@@ -1,21 +1,21 @@\n-%601234567890-=%5B%5D%5C;',./\n+~!@#$%25%5E&*()_+%7B%7D%7C:%22%3C%3E?\n",
            patches.ToPatchString());

        AssertEquals(
            "patch_toText: Object encoding with special separator.",
            "@@ -1,21 +1,21 @@\n-%25257E%2560%25257E~%25257E1%25257E~%25257E2%25257E~%25257E3%25257E~%25257E4%25257E~%25257E5%25257E~%25257E6%25257E~%25257E7%25257E~%25257E8%25257E~%25257E9%25257E~%25257E0%25257E~%25257E-%25257E~%25257E=%25257E~%25257E%255B%25257E~%25257E%255D%25257E~%25257E%255C%25257E~%25257E;%25257E~%25257E'%25257E~%25257E,%25257E~%25257E.%25257E~%25257E/%25257E\n+%25257E%25257E%25257E~%25257E!%25257E~%25257E@%25257E~%25257E#%25257E~%25257E$%25257E~%25257E%2525%25257E~%25257E%255E%25257E~%25257E&%25257E~%25257E*%25257E~%25257E(%25257E~%25257E)%25257E~%25257E_%25257E~%25257E+%25257E~%25257E%257B%25257E~%25257E%257D%25257E~%25257E%257C%25257E~%25257E:%25257E~%25257E%2522%25257E~%25257E%253C%25257E~%25257E%253E%25257E~%25257E?%25257E\n",
            patches.ToPatchString(c => "~" + c.ToString() + "~", '~'));

        diffs = new Rope<Diff<char>>(new[] {
        new Diff<char>(Operation.Delete, "`1234567890-=[]\\;',./"),
        new Diff<char>(Operation.Insert, "~!@#$%^&*()_+{}|:\"<>?")});
        AssertEquals("patch_fromText: Character decoding.",
            diffs,
            Patches.Parse("@@ -1,21 +1,21 @@\n-%601234567890-=%5B%5D%5C;',./\n+~!@#$%25%5E&*()_+%7B%7D%7C:%22%3C%3E?\n".ToRope())[0].Diffs);

        text1 = "";
        for (int x = 0; x < 100; x++)
        {
            text1 += "abcdef";
        }
        text2 = text1 + "123";
        expectedPatch = "@@ -573,28 +573,31 @@\n cdefabcdefabcdefabcdefabcdef\n+123\n";
        patches = Patches.Create(text1, text2);
        AssertEquals("patch_make: Long string with repeats.", expectedPatch, patches.ToPatchString());

        // Test null inputs -- not needed because nulls can't be passed in C#.
    }

    [TestMethod]
    public void PatchSplitMaxTest()
    {
        // Assumes that Match_MaxBits is 32.
        IEnumerable<Patch<char>> patches;

        patches = Patches.Create("abcdefghijklmnopqrstuvwxyz01234567890", "XabXcdXefXghXijXklXmnXopXqrXstXuvXwxXyzX01X23X45X67X89X0");
        patches = patches.PatchSplitMaxLength(this.PatchOptions);
        AssertEquals("patch_splitMax: #1.", "@@ -1,32 +1,46 @@\n+X\n ab\n+X\n cd\n+X\n ef\n+X\n gh\n+X\n ij\n+X\n kl\n+X\n mn\n+X\n op\n+X\n qr\n+X\n st\n+X\n uv\n+X\n wx\n+X\n yz\n+X\n 012345\n@@ -25,13 +39,18 @@\n zX01\n+X\n 23\n+X\n 45\n+X\n 67\n+X\n 89\n+X\n 0\n", patches.ToPatchString());

        patches = Patches.Create("abcdef1234567890123456789012345678901234567890123456789012345678901234567890uvwxyz", "abcdefuvwxyz");
        var oldToText = patches.ToPatchString();
        patches = patches.PatchSplitMaxLength(this.PatchOptions);
        AssertEquals("patch_splitMax: #2.", oldToText, patches.ToPatchString());

        patches = Patches.Create("1234567890123456789012345678901234567890123456789012345678901234567890", "abc");
        patches = patches.PatchSplitMaxLength(this.PatchOptions);
        AssertEquals("patch_splitMax: #3.", "@@ -1,32 +1,4 @@\n-1234567890123456789012345678\n 9012\n@@ -29,32 +1,4 @@\n-9012345678901234567890123456\n 7890\n@@ -57,14 +1,3 @@\n-78901234567890\n+abc\n", patches.ToPatchString());

        patches = Patches.Create("abcdefghij , h : 0 , t : 1 abcdefghij , h : 0 , t : 1 abcdefghij , h : 0 , t : 1", "abcdefghij , h : 1 , t : 1 abcdefghij , h : 1 , t : 1 abcdefghij , h : 0 , t : 1");
        patches = patches.PatchSplitMaxLength(this.PatchOptions);
        AssertEquals("patch_splitMax: #4.", "@@ -2,32 +2,32 @@\n bcdefghij , h : \n-0\n+1\n  , t : 1 abcdef\n@@ -29,32 +29,32 @@\n bcdefghij , h : \n-0\n+1\n  , t : 1 abcdef\n", patches.ToPatchString());
    }

    [TestMethod]
    public void PatchAddPaddingTest()
    {
        Rope<Patch<char>> patches = Patches.Create("", "test");
        AssertEquals("patch_addPadding: Both edges full.",
            "@@ -0,0 +1,4 @@\n+test\n",
            patches.ToPatchString());
        (_, patches) = patches.PatchAddPadding(this.PatchOptions);
        AssertEquals("patch_addPadding: Both edges full.",
            "@@ -1,8 +1,12 @@\n %01%02%03%04\n+test\n %01%02%03%04\n",
            patches.ToPatchString());

        patches = Patches.Create("XY", "XtestY");
        AssertEquals("patch_addPadding: Both edges partial.",
            "@@ -1,2 +1,6 @@\n X\n+test\n Y\n",
            patches.ToPatchString());
        (_, patches) = patches.PatchAddPadding(this.PatchOptions);
        AssertEquals("patch_addPadding: Both edges partial.",
            "@@ -2,8 +2,12 @@\n %02%03%04X\n+test\n Y%01%02%03\n",
            patches.ToPatchString());

        patches = Patches.Create("XXXXYYYY", "XXXXtestYYYY");
        AssertEquals("patch_addPadding: Both edges none.",
            "@@ -1,8 +1,12 @@\n XXXX\n+test\n YYYY\n",
            patches.ToPatchString());
        (_, patches) = patches.PatchAddPadding(this.PatchOptions);
        AssertEquals("patch_addPadding: Both edges none.",
            "@@ -5,8 +5,12 @@\n XXXX\n+test\n YYYY\n",
            patches.ToPatchString());
    }

    [TestMethod]
    public void PatchApplyTest()
    {
        this.PatchOptions = this.PatchOptions with
        {
            MatchDistance = 1000,
            MatchThreshold = 0.5f,
            DeleteThreshold = 0.5f
        };
        IEnumerable<Patch<char>> patches;
        patches = Patches.Create("", "", this.PatchOptions);
        (string Text, bool[] Applied) results = patches.ApplyPatches("Hello world.", this.PatchOptions);
        bool[] boolArray = results.Applied;
        string resultStr = results.Text + "\t" + boolArray.Length;
        AssertEquals("patch_apply: Null case.", "Hello world.\t0", resultStr);

        patches = Patches.Create("The quick brown fox jumps over the lazy dog.", "That quick brown fox jumped over a lazy dog.", this.PatchOptions);
        results = patches.ApplyPatches("The quick brown fox jumps over the lazy dog.", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Exact match.", "That quick brown fox jumped over a lazy dog.\tTrue\tTrue", resultStr);

        results = patches.ApplyPatches("The quick red rabbit jumps over the tired tiger.", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Partial match.", "That quick red rabbit jumped over a tired tiger.\tTrue\tTrue", resultStr);

        results = patches.ApplyPatches("I am the very model of a modern major general.", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Failed match.", "I am the very model of a modern major general.\tFalse\tFalse", resultStr);

        patches = Patches.Create("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy", this.PatchOptions);
        results = patches.ApplyPatches("x123456789012345678901234567890-----++++++++++-----123456789012345678901234567890y", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Big delete, small change.", "xabcy\tTrue\tTrue", resultStr);

        patches = Patches.Create("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy", this.PatchOptions);
        results = patches.ApplyPatches("x12345678901234567890---------------++++++++++---------------12345678901234567890y", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Big delete, big change 1.", "xabc12345678901234567890---------------++++++++++---------------12345678901234567890y\tFalse\tTrue", resultStr);

        this.PatchOptions = this.PatchOptions with { DeleteThreshold = 0.6f };
        patches = Patches.Create("x1234567890123456789012345678901234567890123456789012345678901234567890y", "xabcy", this.PatchOptions);
        results = patches.ApplyPatches("x12345678901234567890---------------++++++++++---------------12345678901234567890y", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Big delete, big change 2.", "xabcy\tTrue\tTrue", resultStr);
        this.PatchOptions = this.PatchOptions with
        {
            DeleteThreshold = 0.5f,
            MatchDistance = 0,
            MatchThreshold = 0.0f
        };
        patches = Patches.Create("abcdefghijklmnopqrstuvwxyz--------------------1234567890", "abcXXXXXXXXXXdefghijklmnopqrstuvwxyz--------------------1234567YYYYYYYYYY890", this.PatchOptions);
        results = patches.ApplyPatches("ABCDEFGHIJKLMNOPQRSTUVWXYZ--------------------1234567890", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0] + "\t" + boolArray[1];
        AssertEquals("patch_apply: Compensate for failed patch.", "ABCDEFGHIJKLMNOPQRSTUVWXYZ--------------------1234567YYYYYYYYYY890\tFalse\tTrue", resultStr);
        this.PatchOptions = this.PatchOptions with
        {
            MatchDistance = 1000,
            MatchThreshold = 0.5f
        };

        patches = Patches.Create("", "test", this.PatchOptions);
        var patchStr = patches.ToPatchString();
        _ = patches.ApplyPatches("", this.PatchOptions);
        AssertEquals("patch_apply: No side effects.", patchStr, patches.ToPatchString());

        patches = Patches.Create("The quick brown fox jumps over the lazy dog.", "Woof", this.PatchOptions);
        patchStr = patches.ToPatchString();
        _ = patches.ApplyPatches("The quick brown fox jumps over the lazy dog.", this.PatchOptions);
        AssertEquals("patch_apply: No side effects with major delete.", patchStr, patches.ToPatchString());

        patches = Patches.Create("", "test", this.PatchOptions);
        results = patches.ApplyPatches("", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0];
        AssertEquals("patch_apply: Edge exact match.", "test\tTrue", resultStr);

        patches = Patches.Create("XY", "XtestY", this.PatchOptions);
        results = patches.ApplyPatches("XY", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0];
        AssertEquals("patch_apply: Near edge exact match.", "XtestY\tTrue", resultStr);

        patches = Patches.Create("y", "y123", this.PatchOptions);
        results = patches.ApplyPatches("x", this.PatchOptions);
        boolArray = results.Applied;
        resultStr = results.Text + "\t" + boolArray[0];
        AssertEquals("patch_apply: Edge partial match.", "x123\tTrue", resultStr);
    }

    private static void AssertEquals(string error_msg, Rope<char> expected, Rope<char> actual) => AssertEquals(error_msg, expected.ToString(), actual.ToString());

    private static void AssertEquals(string error_msg, string expected, string actual)
    {
        if (expected != actual)
        {
            throw new ArgumentException(string.Format("AssertEquals (string, string) fail:\n Expected: {0}\n Actual: {1}\n{2}", expected, actual, error_msg));
        }
    }

    private static void AssertEquals(string error_msg, HalfMatch<char> expected, HalfMatch<char>? actual) => AssertEquals(
        error_msg,
        new Rope<char>[] { expected.FirstPrefix, expected.FirstSuffix, expected.SecondPrefix, expected.SecondSuffix, expected.Common },
        actual is HalfMatch<char> a ? new Rope<char>[] { a.FirstPrefix, a.FirstSuffix, a.SecondPrefix, a.SecondSuffix, a.Common } : null);

    private static void AssertEquals(string error_msg, Rope<char>[] expected, Rope<char>[]? actual)
    {
        if (actual is null)
        {
            throw new ArgumentNullException(nameof(actual));
        }

        if (expected.Length != actual.Length)
        {
            throw new ArgumentException(string.Format("AssertEquals (string[], string[]) length fail:\n Expected: {0}\n Actual: {1}\n{2}", expected, actual, error_msg));
        }
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                throw new ArgumentException(string.Format("AssertEquals (string[], string[]) index {0} fail:\n Expected: {1}\n Actual: {2}\n{3}", i, expected, actual, error_msg));
            }
        }
    }

    private static void AssertEquals(string error_msg, Rope<Rope<char>> expected, Rope<Rope<char>> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new ArgumentException(string.Format("AssertEquals (List<string>, List<string>) length fail:\n Expected: {0}\n Actual: {1}\n{2}", expected, actual, error_msg));
        }
        for (int i = 0; i < expected.Count; i++)
        {
            if (expected[i] != actual[i])
            {
                throw new ArgumentException(string.Format("AssertEquals (List<string>, List<string>) index {0} fail:\n Expected: {1}\n Actual: {2}\n{3}", i, expected, actual, error_msg));
            }
        }
    }

    private static void AssertEquals(string error_msg, IReadOnlyList<Diff<char>> expected, IReadOnlyList<Diff<char>> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new ArgumentException(string.Format("AssertEquals (List<Diff<char>>, List<Diff<char>>) length fail:\n Expected: {0}\n Actual: {1}\n{2}", expected, actual, error_msg));
        }
        for (int i = 0; i < expected.Count; i++)
        {
            if (!expected[i].Equals(actual[i]))
            {
                throw new ArgumentException(string.Format("AssertEquals (List<Diff<char>>, List<Diff<char>>) index {0} fail:\n Expected: {1}\n Actual: {2}\n{3}", i, expected, actual, error_msg));
            }
        }
    }

    private static void AssertEquals(string error_msg, Diff<char> expected, Diff<char> actual)
    {
        if (!expected.Equals(actual))
        {
            throw new ArgumentException(string.Format("assertEquals (Diff, Diff) fail:\n Expected: {0}\n Actual: {1}\n{2}", expected, actual, error_msg));
        }
    }

    private static void AssertEquals<TKey, TValue>(string error_msg, Dictionary<TKey, TValue> expected, Dictionary<TKey, TValue> actual)
        where TKey : notnull
        where TValue : IEquatable<TValue>
    {
        foreach (var k in actual.Keys)
        {
            if (!expected.ContainsKey(k))
            {
                throw new ArgumentException(string.Format("AssertEquals (Dictionary<char, int>, Dictionary<char, int>) key {0} fail:\n Expected: {1}\n Actual: {2}\n{3}", k, "(Missing)", actual[k], error_msg));
            }
        }

        foreach (var k in expected.Keys)
        {
            if (!actual.ContainsKey(k))
            {
                throw new ArgumentException(string.Format("AssertEquals (Dictionary<char, int>, Dictionary<char, int>) key {0} fail:\n Expected: {1}\n Actual: {2}\n{3}", k, expected[k], "(Missing)", error_msg));
            }
            if (!EqualityComparer<TValue>.Default.Equals(actual[k], expected[k]))
            {
                throw new ArgumentException(string.Format("AssertEquals (Dictionary<char, int>, Dictionary<char, int>) key {0} fail:\n Expected: {1}\n Actual: {2}\n{3}", k, expected[k], actual[k], error_msg));
            }
        }
    }

    private static void AssertEquals(string error_msg, long expected, long actual)
    {
        if (expected != actual)
        {
            throw new ArgumentException(string.Format("AssertEquals (int, int) fail:\n Expected: {0}\n Actual: {1}\n{2}", expected, actual, error_msg));
        }
    }

    private static void AssertTrue(string error_msg, bool expected)
    {
        if (!expected)
        {
            throw new ArgumentException(string.Format("AssertTrue fail:\n{0}", error_msg));
        }
    }

    private static void AssertNull(string error_msg, object? value)
    {
        if (value != null)
        {
            throw new ArgumentException(string.Format("AssertNull fail:\n{0}", error_msg));
        }
    }

    private static void AssertFail(string error_msg)
    {
        throw new ArgumentException(string.Format("AssertFail fail:\n{0}", error_msg));
    }
}