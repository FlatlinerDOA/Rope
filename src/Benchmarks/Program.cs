using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.AddExporter(RPlotExporter.Default);
            var summary = BenchmarkRunner.Run(
                new[] { typeof(RopeVersusStringBuilder), typeof(RopeVersusList) },
                config,
                args);

            // Use this to select benchmarks from the console:
            ////var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}