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
            //var s = Stopwatch.StartNew();
            ////new DiffOnLongText().SpeedTest();

            // //var test = new IndexOf();
            // var test = new Equals() { Length = 10 };
            // for (var i = 0; i< 100000;i++)
            // {
            //     test.RopeOfChar();
            // }

            //Console.WriteLine(s.Elapsed);
        }
    }
}