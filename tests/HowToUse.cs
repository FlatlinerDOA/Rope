namespace Rope.UnitTests;

using Rope.Compare;

[TestClass]
public class HowToUse
{
    [TestMethod]
    public void Introduction()
    {
        // Converting to a Rope<char> doesn't allocate any strings (simply points to the original memory).
        Rope<char> myRope1 = "My favourite text".ToRope();

        // In fact strings are implicitly convertible to Rope<char>, this makes Rope<char> a drop in replacement for `string` in most cases;
        Rope<char> myRope2 = "My favourite text";

        // With Rope<T>, splits don't allocate any new strings either.
        IEnumerable<Rope<char>> words = myRope2.Split(' ');

        // Calling ToString() allocates a new string at the time of conversion.
        Console.WriteLine(words.First().ToString());

        // Warning: Assigning a Rope<char> to a string requires calling .ToString() and hence copying memory.
        string text2 = (myRope2 + " My second favourite text").ToString();

        // Better: Assigning to another rope makes a new rope out of the other two ropes, no string allocations or copies.
        Rope<char> text3 = myRope2 + " My second favourite text";

        // Value-like equivalence, no need for SequenceEqual!.
        Assert.IsTrue("test".ToRope() == ("te".ToRope() + "st".ToRope()));
        Assert.IsTrue("test".ToRope().GetHashCode() == ("te".ToRope() + "st".ToRope()).GetHashCode());

        // Store a rope of anything, just like List<T>! 
        // This makes an immutable and thread safe list of people.
        Rope<Person> ropeOfPeople = [new Person("Frank", "Stevens"), new Person("Jane", "Seymour")];
    }

    [TestMethod]
    public void CreateDiffOfStrings()
    {
        Rope<char> sourceText = "abcdef";
        Rope<char> targetText = "abefg";

        // Create a list of differences.
        Rope<Diff<char>> diffs = sourceText.Diff(targetText);

        // Recover either side from the list of differences.
        Rope<char> recoveredSourceText = diffs.ToSource();
        Rope<char> recoveredTargetText = diffs.ToTarget();

        // Create a Patch string (like Git's patch text)
        Rope<Patch<char>> patches = diffs.ToPatches(); // A list of patches
        Rope<char> patchText = patches.ToPatchString(); // A single string of patch text.
        Console.WriteLine(patchText);
        /** Outputs:
        @@ -1,6 +1,5 @@
         ab
        -cd
         ef
        +g
        */

        // Parse out the patches from the patch text.
        Rope<Patch<char>> parsedPatches = Patches.Parse(patchText);
    }

    [TestMethod]
    public void CreateDiffsOfAnything()
    {
        Rope<Person> original =
        [
            new Person("Stephen", "King"),
            new Person("Jane", "Austen"),
            new Person("Mary", "Shelley"),
            new Person("JRR", "Tokien"),
            new Person("James", "Joyce"),
        ];

        Rope<Person> updated =
        [
            new Person("Stephen", "King"),
            new Person("Jane", "Austen"),
            new Person("JRR", "Tokien"),
            new Person("Frank", "Miller"),
            new Person("George", "Orwell"),
            new Person("James", "Joyce"),
        ];

        Rope<Diff<Person>> changes = original.Diff(updated, DiffOptions<Person>.Default);
        Assert.AreEqual(2, changes.Count(d => d.Operation != Operation.Equal));

        // Convert to a Delta string
        Rope<char> delta = changes.ToDelta(p => p.ToString());

        // Rebuild the diff from the original list and a delta.
        Rope<Diff<Person>> fromDelta = Delta.Parse(delta, original, Person.Parse);

        // Get back the original list
        Assert.AreEqual(fromDelta.ToSource(), original);

        // Get back the updated list.
        Assert.AreEqual(fromDelta.ToTarget(), updated);

        // Make a patch text
        Rope<Patch<Person>> patches = fromDelta.ToPatches();

        // Convert patches to text
        Rope<char> patchText = patches.ToPatchString(p => p.ToString());

        // Parse the patches back again
        Rope<Patch<Person>> parsedPatches = Patches.Parse(patchText, Person.Parse);
        Assert.AreEqual(parsedPatches, patches);
    }

    private record Person(Rope<char> FirstName, Rope<char> LastName)
    {
        public override string ToString() => $"{FirstName} {LastName}";

        public static Person Parse(Rope<char> source) => new(source.Split(' ').FirstOrDefault(), source.Split(' ').Skip(1).FirstOrDefault());
    }
}
