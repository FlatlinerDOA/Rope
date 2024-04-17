using BenchmarkDotNet.Attributes;
using Rope;
using Rope.Compare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{    
    public class DiffLineHashing
    {
        [Benchmark]
        public void LinesToCharsPure()
        {
            var b = new LinesToCharsBenchTest();
            b.Run();
        }

        private class LinesToCharsBenchTest : DiffMatchPatch
        {
            public void Run()
            {
                // More than 65536 to verify any 16-bit limitation.
                var lineList = Rope<char>.Empty;
                for (int i = 0; i < 66000; i++)
                {
                    lineList = lineList.AddRange((i + "\n").AsMemory());
                }

                lineList = lineList.Balanced();
                var result = this.diff_linesToChars_pure(lineList, Rope<char>.Empty);
            }
        }
    }
}
