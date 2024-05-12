using System;
using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using Rope;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.AddExporter(RPlotExporter.Default);
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
            
            // var s = Stopwatch.StartNew();
            // var test = new DiffOnLongText();

            // //// //var test = new IndexOf();
            // //// var test = new Equals() { Length = 10 };
            // for (var i = 0; i < 1; i++)
            // {
            // //    //     test.RopeOfChar();
            //     test.RopeOfCharDiff();
            // }

            // Console.WriteLine(s.Elapsed);
        }
    }
}