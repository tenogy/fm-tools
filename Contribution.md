## Manual installation of tools

To manually install the tools, follow these steps:

1. In the terminal, go to the `fm-tools` directory.
2. Pack projects into NuGet packages using the commands:
```shell
dotnet pack ./src/Tenogy.Tools.FluentMigrator.AddMigration &
dotnet pack ./src/Tenogy.Tools.FluentMigrator.UpdateDatabase
```
3. Install the tools using the commands:
```shell
dotnet tool install --global --add-source ./dist/nupkg Tenogy.Tools.FluentMigrator.AddMigration &
dotnet tool install --global --add-source ./dist/nupkg Tenogy.Tools.FluentMigrator.UpdateDatabase
```
4. If you need to remove the tools, use the commands:
```shell
dotnet tool uninstall --global Tenogy.Tools.FluentMigrator.AddMigration &
dotnet tool uninstall --global Tenogy.Tools.FluentMigrator.UpdateDatabase
```
5. Check the functionality of the tools using the commands:
```shell
add-migration --help &
update-database --help
```
