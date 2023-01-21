# Tenogy.Tools.FluentMigrator

Tools for dotnet which helps creation an updating database migrations based on FluentMigrator.

**Available tools:**

1. `add-migration` – Creates a migration for the database in your project/solution.
2. `update-database` – Performs a database update, or generates an SQL query.

## add-migration tool

Check last version on [nuget.org](https://www.nuget.org/packages/Tenogy.Tools.FluentMigrator.AddMigration)
```shell
dotnet tool install --global Tenogy.Tools.FluentMigrator.AddMigration --version 1.0.2
```
For help:
```shell
add-migration --help
```
### Usage

Open your project or solution in your favorite IDE and run a terminal inside it. Make sure that the working directory is a directory inside your project or solution.
```shell
add-migration CreateTableTest
```

### Options

| Option              | Description                                    |
|-------------------|------------------------------------------------|
| `-s`, `--silent`  | Do not open the migration file after creation. |
| `-v`, `--verbose` | Enable logging.                                |


## update-database tool

Check last version on [nuget.org](https://www.nuget.org/packages/Tenogy.Tools.FluentMigrator.UpdateDatabase)
```shell
dotnet tool install --global Tenogy.Tools.FluentMigrator.UpdateDatabase --version 1.0.2
```
For help:
```shell
update-database --help
```
### Usage

Open your project or solution in your favorite IDE and run a terminal inside it. Make sure that the working directory is a directory inside your project or solution.

Example of launching the tool:

```shell
update-database
```

### Options

| Option                        | Description                                                                         |
|-----------------------------|-------------------------------------------------------------------------------------|
| `-s`, `--script`            | Generate an SQL file without updating the database.                                 |
| `-f`, `--files`             | Split the generated SQL file into migration files (@scripts directory).             |
| `-a`, `--assembly`          | The path to the assembled FluentMigrator project.                                   |
| `-c`, `--connection-string` | Database connection string or key of ConnectionStrings section in appsettings.json. |
| `-d`, `--database`          | The type of database. Available options: PostgreSql, SqlServer, SqlLite.            |
| `-v`, `--verbose`           | Enable logging.                                                                     |


