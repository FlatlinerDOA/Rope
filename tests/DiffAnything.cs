using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rope.UnitTests
{
    using Rope.Compare;

    [TestClass]
    public class DiffAnything
    {
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
            Rope<Patch<Person>> parsedPatches = Patches.Parse(patchText.ToRope(), Person.Parse);
            Assert.AreEqual(parsedPatches, patches);
        }

        private record Person(Rope<char> FirstName, Rope<char> LastName)
        {
            public override string ToString() => $"{FirstName} {LastName}";

            public static Person Parse(Rope<char> source) => new(source.Split(' ').FirstOrDefault(), source.Split(' ').Skip(1).FirstOrDefault());
        }
    }
}
