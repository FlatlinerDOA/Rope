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

            var changes = original.Diff(updated, DiffOptions<Person>.Default);
            Assert.AreEqual(2, changes.Count(d => d.Operation != Operation.Equal));

            // Convert to a Delta string
            var delta = changes.ToDelta(p => p.ToString());

            // Rebuild the diff from the original list and a delta.
            var fromDelta = delta.ParseDelta(original, Person.Parse);

            // Get back the original list
            Assert.AreEqual(fromDelta.ToSource(), original);

            // Get back the updated list.
            Assert.AreEqual(fromDelta.ToTarget(), updated);

            // Make a patch text
            var patches = fromDelta.ToPatches();

            // TODO: Convert patches to text
            //var patchText = patches.ToPatchText(p => p.ToString());

            // TODO: Parse the patches back again
            //var parsedPatches = patchText.ToRope().ParsePatchText(Person.Parse);
            //Assert.AreEqual(parsedPatches, patches);
        }

        private record Person(Rope<char> FirstName, Rope<char> LastName)
        {
            public override string ToString() => $"{FirstName} {LastName}";

            public static Person Parse(Rope<char> source) => new(source.Split(' ').FirstOrDefault(), source.Split(' ').Skip(1).FirstOrDefault());
        }
    }
}
