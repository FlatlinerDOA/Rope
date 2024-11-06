@ECHO OFF
dotnet.exe run -c Release --filter *
xcopy .\bin\Release\net8.0\BenchmarkDotNet.Artifacts\results\*-github.md .\results  /Y
xcopy .\bin\Release\net8.0\BenchmarkDotNet.Artifacts\results\*-barplot.png .\results /Y