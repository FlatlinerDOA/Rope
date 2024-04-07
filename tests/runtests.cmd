dotnet coverage collect -f cobertura -o CodeCoverage\code-coverage.xml dotnet test --logger "console;verbosity=detailed"
reportgenerator -reports:"CodeCoverage\code-coverage.xml" -targetdir:"CodeCoverage" -reporttypes:Html
CodeCoverage\index.html