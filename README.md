# Tenogy.Tools.FluentMigrator

Tools (commands) for dotnet, with which you can manage migrations in the database. Tools are alternatives for CmdLets. 

**Available tools:**

1. `Add-Migration` – Creates a migration for the database in your project/solution.
2. `Update-Database` – Performs a database update, or generates an SQL query.


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


## Add-Migration tool

Creates a migration for the database in your project/solution.

### Guide

Open your project or solution in your favorite IDE and run a terminal inside it. Make sure that the working directory is a directory inside your project or solution.

Example of launching the tool:

```shell
add-migration CreateTableTest
```

### Flags

| Flag              | Description                                    |
|-------------------|------------------------------------------------|
| `-s`, `--silent`  | Do not open the migration file after creation. |
| `-v`, `--verbose` | Enable logging.                                |

All flags are optional.

## Update-Database tool

Performs a database update, or generates an SQL query.

### Guide

Open your project or solution in your favorite IDE and run a terminal inside it. Make sure that the working directory is a directory inside your project or solution.

Example of launching the tool:

```shell
update-database
```

### Flags

| Flag                        | Description                                                                         |
|-----------------------------|-------------------------------------------------------------------------------------|
| `-s`, `--script`            | Generate an SQL file without updating the database.                                 |
| `-a`, `--assembly`          | The path to the assembled FluentMigrator project.                                   |
| `-c`, `--connection-string` | Database connection string or key of ConnectionStrings section in appsettings.json. |
| `-d`, `--database`          | The type of database. Available options: PostgreSql, SqlServer, SqlLite.            |
| `-v`, `--verbose`           | Enable logging.                                                                     |

All flags are optional.
