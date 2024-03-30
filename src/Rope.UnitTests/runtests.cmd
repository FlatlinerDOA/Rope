dotnet coverage collect -f cobertura -o CodeCoverage\code-coverage.xml dotnet test
reportgenerator -reports:"CodeCoverage\code-coverage.xml" -targetdir:"CodeCoverage" -reporttypes:Html
CodeCoverage\index.html