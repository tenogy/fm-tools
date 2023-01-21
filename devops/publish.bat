set NugetDir=C:/nuget
set NugetSrc=https://api.nuget.org/v3/index.json
set /p Key=<%NugetDir%/key.txt

dotnet nuget push %NugetDir%/Tenogy.Tools.FluentMigrator.AddMigration.1.0.2.nupkg -k %Key% -s %NugetSrc%
dotnet nuget push %NugetDir%/Tenogy.Tools.FluentMigrator.UpdateDatabase.1.0.2.nupkg -k %Key% -s %NugetSrc%