using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tenogy.Tools.FluentMigrator.Helpers;
using Tenogy.Tools.FluentMigrator.Services;
using Tenogy.Tools.FluentMigrator.UpdateDatabase.Options;
using Tenogy.Tools.FluentMigrator.UpdateDatabase.Services;

namespace Tenogy.Tools.FluentMigrator.UpdateDatabase;

internal interface IUpdateDatabaseTool
{
	Task Update(string? assemblyPath, string? processorType, string? connectionString);

	Task<FileInfo> UpdateAndOpen(string? assemblyPath, string? processorType, string? connectionString, bool scriptFiles);
}

internal sealed class UpdateDatabaseTool : IUpdateDatabaseTool
{
	private readonly IProjectAssemblySearchService _projectAssemblySearchService;
	private readonly IProjectAppSettingsService _projectAppSettingsService;
	private readonly IDatabaseProcessorTypeService _databaseProcessorTypeService;
	private readonly IFluentMigratorRunnerService _fluentMigratorRunnerService;
	private readonly IOpenFileService _openFileService;

	public static readonly UpdateDatabaseTool Default = new(null, null, null, null, null);

	public UpdateDatabaseTool(
		IProjectAssemblySearchService? projectAssemblySearchService,
		IProjectAppSettingsService? projectAppSettingsService,
		IDatabaseProcessorTypeService? databaseProcessorTypeService,
		IFluentMigratorRunnerService? fluentMigratorRunnerService,
		IOpenFileService? openFile
	)
	{
		_projectAssemblySearchService = projectAssemblySearchService ?? ProjectAssemblySearchService.Default;
		_projectAppSettingsService = projectAppSettingsService ?? ProjectAppSettingsService.Default;
		_databaseProcessorTypeService = databaseProcessorTypeService ?? DatabaseProcessorTypeService.Default;
		_fluentMigratorRunnerService = fluentMigratorRunnerService ?? FluentMigratorRunnerService.Default;
		_openFileService = openFile ?? OpenFileService.Default;
	}

	public async Task Update(string? assemblyPath, string? processorType, string? connectionString)
	{
		var (projectAssembly, _) = await _projectAssemblySearchService.Search(assemblyPath, false);

		connectionString = await _projectAppSettingsService.GetConnectionString(projectAssembly.Directory!.FullName, connectionString);
		processorType = _databaseProcessorTypeService.Get(connectionString, processorType);

		var fluentMigratorRunnerOptions = CreateUpdateDatabaseOptions(projectAssembly, processorType, connectionString, false);

		ConsoleLogger.LogDebug("Trying to update database of '{AssemblyPath}'...", projectAssembly.FullName);

		_fluentMigratorRunnerService.Run(fluentMigratorRunnerOptions);

		ConsoleColored.WriteSuccessLine("The database update was completed successfully.");
		ConsoleLogger.LogInformation("The database update was completed successfully");
	}

	public async Task<FileInfo> UpdateAndOpen(string? assemblyPath, string? processorType, string? connectionString, bool scriptFiles)
	{
		var (projectAssembly, project) = await _projectAssemblySearchService.Search(assemblyPath, false);

		connectionString = await _projectAppSettingsService.GetConnectionString(projectAssembly.Directory!.FullName, connectionString);
		processorType = _databaseProcessorTypeService.Get(connectionString, processorType);

		var fluentMigratorRunnerOptions = CreateUpdateDatabaseOptions(projectAssembly, processorType, connectionString, true);

		ConsoleLogger.LogDebug("Trying to generate SQL file of assembly '{AssemblyPath}'...", projectAssembly.FullName);

		_fluentMigratorRunnerService.Run(fluentMigratorRunnerOptions);

		var result = new FileInfo(fluentMigratorRunnerOptions.OutputFileName!);

		if (scriptFiles)
			await OpenFiles(result, project, processorType);
		else
			await _openFileService.Open(result);

		ConsoleColored.WriteSuccessLine("The SQL file generation was completed successfully.");
		ConsoleLogger.LogInformation("The SQL file generation was completed successfully");

		return result;
	}

	private static FluentMigratorRunnerOptions CreateUpdateDatabaseOptions(FileInfo projectAssembly, string processorType, string connectionString, bool createSqlFile)
	{
		var fluentMigratorRunnerOptions = new FluentMigratorRunnerOptions
		{
			TargetAssembly = projectAssembly.FullName,
			ProcessorType = processorType,
			ConnectionString = connectionString
		};

		if (createSqlFile)
		{
			var outputFile = new FileInfo(Path.Combine(
				projectAssembly.Directory!.FullName,
				Path.ChangeExtension(projectAssembly.Name, "sql")
			));

			fluentMigratorRunnerOptions.Output = true;
			fluentMigratorRunnerOptions.PreviewOnly = true;
			fluentMigratorRunnerOptions.OutputFileName = outputFile.FullName;
		}

		return fluentMigratorRunnerOptions;
	}

	private async Task OpenFiles(FileInfo outputFile, FileInfo? project, string? processorType)
	{
		FileInfo[] files;

		try
		{
			files = await SplitOutputFile(outputFile, project, processorType) ?? new[] { outputFile };
		}
		catch (Exception e)
		{
			ConsoleColored.WriteDanger(e.ToString());
			files = new[] { outputFile };
		}

		foreach (var file in files)
			await _openFileService.Open(file);
	}

	private async Task<FileInfo[]?> SplitOutputFile(FileInfo outputFile, FileInfo? project, string? processorType)
	{
		if (project?.Exists != true || !outputFile.Exists)
			return null;

		// Find scripts directory

		var scriptsDirectory = new DirectoryInfo(Path.Combine(project.Directory!.FullName, "@Scripts", "Migrations"));

		if (!scriptsDirectory.Exists)
			scriptsDirectory.Create();

		var migrations = new Dictionary<long, (string name, int startAt, int? endAt)>();
		var lines = (await File.ReadAllLinesAsync(outputFile.FullName)).ToArray();
		var regex = new Regex(@"^\/\*\s(\d+):\s(\w+)\s(migrating|migrated)", RegexOptions.IgnoreCase);

		var i = 0;

		// Parse output file

		foreach (var line in lines)
		{
			var match = regex.Match(line);

			if (!match.Success)
			{
				i++;
				continue;
			}

			var version = long.Parse(match.Groups[1].Value);
			var name = match.Groups[2].Value;
			var opt = match.Groups[3].Value;

			switch (opt)
			{
				case "migrating":
					migrations[version] = (name, i, null);
					break;
				case "migrated":
				{
					var m = migrations[version];
					migrations[version] = (m.name, m.startAt, i);
					break;
				}
			}

			i++;
		}

		if (!migrations.Values.Any() || migrations.Values.Any(x => string.IsNullOrWhiteSpace(x.name) || !x.endAt.HasValue))
			return null;

		// Create migrations

		var migrationFiles = new List<FileInfo>();
		var beginTransaction = "BEGIN TRANSACTION";
		var commit = "COMMIT";

		beginTransaction += processorType is "Postgres" or "Sqlite" ? ";" : "";
		commit += processorType is "Postgres" or "Sqlite" ? ";" : "";

		foreach (var (version, (name, startAt, endAt)) in migrations)
		{
			var sql = $@"
{beginTransaction}

{string.Join("\n", lines.Skip(startAt + 1).Take(endAt!.Value - startAt - 1)).Trim()}

{commit}
".Trim();

			var migrationFileName = version + "_" + name + ".sql";
			var migrationFile = new FileInfo(Path.Combine(
				outputFile.Directory!.FullName,
				migrationFileName
			));

			await File.WriteAllTextAsync(migrationFile.FullName, sql);
			migrationFiles.Add(migrationFile);
		}

		// Put migrations to scripts directory

		foreach (var migrationFile in migrationFiles)
		{
			var outputMigrationFile = new FileInfo(Path.Combine(
				scriptsDirectory.FullName,
				migrationFile.Name
			));

			if (outputMigrationFile.Exists)
				outputMigrationFile.Delete();

			migrationFile.MoveTo(outputMigrationFile.FullName);
		}

		return migrationFiles.ToArray();
	}
}
