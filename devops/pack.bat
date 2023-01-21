set NugetDir=C:/nuget

dotnet pack -c Release -o %NugetDir%  ..\src\Tenogy.Tools.FluentMigrator.AddMigration\Tenogy.Tools.FluentMigrator.AddMigration.csproj
dotnet pack -c Release -o %NugetDir%  ..\src\Tenogy.Tools.FluentMigrator.UpdateDatabase\Tenogy.Tools.FluentMigrator.UpdateDatabase.csproj