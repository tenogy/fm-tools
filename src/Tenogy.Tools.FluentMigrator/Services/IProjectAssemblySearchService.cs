using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IProjectAssemblySearchService
{
	Task<FileInfo> Search(string? projectAssemblyPath, bool ignoreFindProject);
}

public sealed class ProjectAssemblySearchService : IProjectAssemblySearchService
{
	private readonly ILogger<ProjectAssemblySearchService>? _logger;
	private readonly IProjectSearchService _projectSearchService;
	private readonly IProjectBuilderService _projectBuilderService;

	public static readonly ProjectAssemblySearchService Default = new(null, null, null);

	public ProjectAssemblySearchService(
		ILogger<ProjectAssemblySearchService>? logger,
		IProjectSearchService? projectFinder,
		IProjectBuilderService? projectBuilder
	)
	{
		_logger = logger;
		_projectSearchService = projectFinder ?? ProjectSearchService.Default;
		_projectBuilderService = projectBuilder ?? ProjectBuilderService.Default;
	}

	public async Task<FileInfo> Search(string? projectAssemblyPath, bool ignoreFindProject)
	{
		_logger?.LogDebug("Trying to find the FluentMigrator project assembly '{ProjectAssemblyPath}'...", projectAssemblyPath);

		if (string.IsNullOrWhiteSpace(projectAssemblyPath) && !ignoreFindProject)
		{
			_logger?.LogDebug("The path to build the project is not specified, trying to find the project and build it...");

			var project = _projectSearchService.Search(Environment.CurrentDirectory);
			projectAssemblyPath = (await _projectBuilderService.Build(project.FullName)).FullName;
		}

		ConsoleColored.WriteMutedLine("Trying to find the project assembly...");

		if (string.IsNullOrWhiteSpace(projectAssemblyPath))
		{
			ConsoleColored.WriteDangerLine("The directory with the assembled project is not specified.");
			throw new ArgumentNullException(nameof(projectAssemblyPath), "The directory with the assembled FluentMigrator project is not specified.");
		}

		var fileInfo = new FileInfo(projectAssemblyPath);

		if (!fileInfo.Exists)
		{
			ConsoleColored.WriteDangerLine("The assembled project was not found.");
			throw new FileNotFoundException($"The assembled FluentMigrator project was not found '{projectAssemblyPath}'.");
		}

		ConsoleColored.WriteInfoLine("Project assembly found: " + fileInfo.Name);
		_logger?.LogInformation("The assembled project was found in the directory '{ProjectAssemblyPath}'", projectAssemblyPath);
		return fileInfo;
	}
}
