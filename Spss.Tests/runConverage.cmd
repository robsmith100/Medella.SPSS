call dotnet test --collect:"XPlat Code Coverage" 
call reportgenerator -reports:**/coverage.cobertura.xml -targetdir:c:\temp\CodeCoverage -reporttypes:HtmlInline_AzurePipelines
call del /s coverage.cobertura.xml
call c:\temp\CodeCoverage\index.htm
