using System.IO;
using System.Threading.Tasks;
using Tenogy.Tools.FluentMigrator.Helpers;
using Tenogy.Tools.FluentMigrator.Services;
using Tenogy.Tools.FluentMigrator.UpdateDatabase.Options;
using Tenogy.Tools.FluentMigrator.UpdateDatabase.Services;

// ReSharper disable once CheckNamespace
namespace Tenogy.Tools.FluentMigrator;

internal interface IUpdateDatabaseTool
{
	Task Update(string? assemblyPath, string? processorType, string? connectionString);

	Task<FileInfo> UpdateAndOpen(string? assemblyPath, string? processorType, string? connectionString);
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
		var projectAssembly = await _projectAssemblySearchService.Search(assemblyPath, false);

		connectionString = await _projectAppSettingsService.GetConnectionString(projectAssembly.Directory!.FullName, connectionString);
		processorType = _databaseProcessorTypeService.Get(connectionString, processorType);

		var fluentMigratorRunnerOptions = CreateUpdateDatabaseOptions(projectAssembly, processorType, connectionString, false);

		ConsoleLogger.LogDebug("Trying to update database of '{AssemblyPath}'...", projectAssembly.FullName);

		_fluentMigratorRunnerService.Run(fluentMigratorRunnerOptions);

		ConsoleColored.WriteSuccessLine("The database update was completed successfully.");
		ConsoleLogger.LogInformation("The database update was completed successfully");
	}

	public async Task<FileInfo> UpdateAndOpen(string? assemblyPath, string? processorType, string? connectionString)
	{
		var projectAssembly = await _projectAssemblySearchService.Search(assemblyPath, false);

		connectionString = await _projectAppSettingsService.GetConnectionString(projectAssembly.Directory!.FullName, connectionString);
		processorType = _databaseProcessorTypeService.Get(connectionString, processorType);

		var fluentMigratorRunnerOptions = CreateUpdateDatabaseOptions(projectAssembly, processorType, connectionString, true);

		ConsoleLogger.LogDebug("Trying to generate SQL file of assembly '{AssemblyPath}'...", projectAssembly.FullName);

		_fluentMigratorRunnerService.Run(fluentMigratorRunnerOptions);

		var result = new FileInfo(fluentMigratorRunnerOptions.OutputFileName!);

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
}
