@ECHO OFF
dotnet.exe run -c Release --filter * --framework net8.0 --runtimes net8.0 
xcopy .\BenchmarkDotNet.Artifacts\results\*-github.md .\results  /Y
xcopy .\BenchmarkDotNet.Artifacts\results\*-barplot.png .\results /Y