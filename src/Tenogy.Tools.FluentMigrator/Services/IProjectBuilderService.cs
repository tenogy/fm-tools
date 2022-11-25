using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IProjectBuilderService
{
	Task<FileInfo> Build(string csProjFilePath);
}

public sealed class ProjectBuilderService : IProjectBuilderService
{
	private readonly ILogger<ProjectBuilderService>? _logger;
	private readonly IProcessRunnerService _processRunnerService;

	public static readonly ProjectBuilderService Default = new(null, null);

	public ProjectBuilderService(
		ILogger<ProjectBuilderService>? logger,
		IProcessRunnerService? processRunner
	)
	{
		_logger = logger;
		_processRunnerService = processRunner ?? ProcessRunnerService.Default;
	}

	public async Task<FileInfo> Build(string csProjFilePath)
	{
		ConsoleColored.WriteMutedLine("Trying to build a project...");
		_logger?.LogDebug("Trying to build a project '{CsProjFilePath}'...", csProjFilePath);

		if (string.IsNullOrWhiteSpace(csProjFilePath))
		{
			ConsoleColored.WriteDangerLine("Project path not specified.");
			throw new ArgumentNullException(nameof(csProjFilePath), "Project path not specified.");
		}

		var csProjFile = new FileInfo(csProjFilePath);

		if (!csProjFile.Exists)
		{
			ConsoleColored.WriteDangerLine("Project file does not exist.");
			throw new FileNotFoundException("Project file does not exist.", csProjFile.FullName);
		}

		const string configuration = "Release";
		var targetFramework = GetProjectTargetFramework(csProjFile);

		var outputPath = new FileInfo(Path.Combine(
			csProjFile.Directory!.FullName,
			"bin",
			configuration,
			targetFramework,
			GetSolutionAssemblyName(csProjFile)
		));

		_logger?.LogDebug("The project will be built into a folder: {ProjectAssemblyPath}", outputPath.Directory!.FullName);

		var (exitCode, output) = await _processRunnerService.RunAndWait("dotnet", string.Join(" ",
			"build",
			$@"""{csProjFile.FullName}""",
			"-c " + configuration
		));

		if (exitCode == 0)
		{
			if (!outputPath.Exists)
			{
				ConsoleColored.WriteDangerLine("The assembled project could not be found.");
				throw new FileNotFoundException("The assembled project could not be found.", outputPath.FullName);
			}

			ConsoleColored.WriteInfoLine("The project was successfully built.");
			_logger?.LogInformation("The project '{ProjectName}' was successfully built", csProjFile.Name);
			return outputPath;
		}

		ConsoleColored.WriteDangerLine($"Failed to build the project due to an error ({exitCode}). Output: {Environment.NewLine}{output}");
		throw new InvalidOperationException($"Failed to build the project `{csProjFile.Name}` due to an error. ExitCode: {exitCode}. Output: {output}");
	}

	private static string GetProjectTargetFramework(FileInfo csProjFile)
	{
		var doc = new XmlDocument();
		doc.Load(csProjFile.FullName);
		return (
				doc.SelectSingleNode("/Project/PropertyGroup/TargetFrameworks")?.InnerText ??
				doc.SelectSingleNode("/Project/PropertyGroup/TargetFramework")?.InnerText ?? ""
			)
			.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			.Last(x => !string.IsNullOrWhiteSpace(x));
	}

	private static string GetSolutionAssemblyName(FileInfo csProjFile)
	{
		var doc = new XmlDocument();
		doc.Load(csProjFile.FullName);
		var assemblyName = doc.SelectSingleNode("/Project/PropertyGroup/AssemblyName")?.InnerText;
		return (string.IsNullOrWhiteSpace(assemblyName) ? Path.GetFileNameWithoutExtension(csProjFile.Name) : assemblyName) + ".dll";
	}
}
