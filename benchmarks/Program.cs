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
            //var config = DefaultConfig.Instance.AddExporter(RPlotExporter.Default);
            //var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
            //new DiffOnShortText().DiffMain();
            new DiffLineHashing().LinesToCharsPure();
            
            //var x = new IndexOf();
            //for (var i = 0; i< 100000;i++)
            //{
            //    x.FragmentedRopeOfCharFillLarge();
            //}
        }
    }
}