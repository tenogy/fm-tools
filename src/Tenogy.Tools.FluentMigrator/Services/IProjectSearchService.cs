using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IProjectSearchService
{
	FileInfo Search(string? rootDirectoryPath);
}

public sealed class ProjectSearchService : IProjectSearchService
{
	public static readonly ProjectSearchService Default = new();

	public ProjectSearchService()
	{
	}

	public FileInfo Search(string? rootDirectoryPath)
	{
		ConsoleColored.WriteMutedLine("Trying to find a project...");
		ConsoleLogger.LogDebug("Trying to find FluentMigrator project in directory '{RootDirectoryPath}'...", rootDirectoryPath);

		if (string.IsNullOrWhiteSpace(rootDirectoryPath))
		{
			ConsoleColored.WriteDangerLine("Root directory path not specified.");
			throw new ArgumentNullException(nameof(rootDirectoryPath), "Root directory path not specified.");
		}

		var rootDirectory = new DirectoryInfo(rootDirectoryPath);

		if (!rootDirectory.Exists)
		{
			ConsoleColored.WriteDangerLine("Root directory not exists.");
			throw new DirectoryNotFoundException($"Root directory '{rootDirectory.FullName}' not exists.");
		}

		if (IsProjectDirectory(rootDirectory))
		{
			ConsoleLogger.LogDebug("Root directory '{RootDirectoryPath}' contains project...", rootDirectory.FullName);
			var fileInfo = rootDirectory.GetFiles("*.csproj").First();

			if (IsFluentMigratorProject(fileInfo) == true)
			{
				ConsoleLogger.LogDebug("And it is a FluentMigrator project!");
				return ReturnResult(fileInfo);
			}

			ConsoleLogger.LogDebug("But it is not FluentMigrator project");
		}

		ConsoleLogger.LogDebug("Trying find solution directory at root of '{RootDirectoryPath}'...", rootDirectory.FullName);
		rootDirectory = TryFindSolutionDirectory(rootDirectory);

		if (rootDirectory == null)
		{
			ConsoleColored.WriteDangerLine("The directory with the solution was not found.");
			throw new DirectoryNotFoundException("The directory with the solution was not found.");
		}

		ConsoleLogger.LogDebug("Trying to find FluentMigrator projects in the directory '{RootDirectoryPath}' with the solution ...", rootDirectory.FullName);
		var fileInfos = TryFindFluentMigratorProjects(rootDirectory);

		if (!fileInfos.Any())
		{
			ConsoleColored.WriteDangerLine("Projects could not be found in the solution directory. Install the NuGet FluentMigrator or Tenogy.FluentMigrator package into your project.");
			throw new FileNotFoundException($"FluentMigrator projects could not be found in the solution directory '{rootDirectory.FullName}'.");
		}

		if (fileInfos.Length > 1)
		{
			var projects = string.Join("; ", fileInfos.Select(x => "'" + x.Name + "'"));
			ConsoleColored.WriteDangerLine($"More than one project was found in the solution directory: {projects}");
			throw new InvalidOperationException($"More than one FluentMigrator project was found in the solution directory '{rootDirectory.FullName}'. Project names: {projects}");
		}

		return ReturnResult(fileInfos.First());

		FileInfo ReturnResult(FileInfo f)
		{
			ConsoleColored.WriteInfoLine($"Project found: {f.Name}");
			ConsoleLogger.LogInformation("The project was found '{ProjectPath}'", f.FullName);
			return f;
		}
	}

	#region Helpers

	private static bool IsProjectDirectory(DirectoryInfo rootDirectory)
		=> Directory.GetFiles(rootDirectory.ToString(), "*.csproj").Any();

	private static bool IsSolutionDirectory(DirectoryInfo rootDirectory)
		=> Directory.GetFiles(rootDirectory.ToString(), "*.sln").Any();

	private bool? IsFluentMigratorProject(FileInfo fileInfo)
	{
		try
		{
			if (fileInfo.Name.StartsWith("Tenogy.Tools.FluentMigrator", StringComparison.OrdinalIgnoreCase)) return false;

			var doc = new XmlDocument();
			doc.Load(fileInfo.FullName);
			var packageReferences = doc.SelectNodes("/Project/ItemGroup/PackageReference");
			if (packageReferences == null || packageReferences.Count == 0) return false;

			for (var i = 0; i < packageReferences.Count; i++)
			{
				var packageReference = packageReferences[i];
				if (packageReference?.Attributes == null) continue;

				foreach (XmlAttribute attribute in packageReference.Attributes)
				{
					var name = attribute?.Name ?? "";
					var value = attribute?.Value ?? "";

					if (name.Equals("Include", StringComparison.OrdinalIgnoreCase) != true) continue;
					if (value.StartsWith("FluentMigrator", StringComparison.OrdinalIgnoreCase)) return true;
					if (value.StartsWith("Tenogy.FluentMigrator", StringComparison.OrdinalIgnoreCase)) return true;
					if (value.StartsWith("Tenogy.App.FluentMigrator", StringComparison.OrdinalIgnoreCase)) return true;
				}
			}

			return false;
		}
		catch (Exception e)
		{
			ConsoleLogger.LogWarning(e, "Can't define that project is FluentMigrator");
			return null;
		}
	}

	private static DirectoryInfo? TryFindSolutionDirectory(DirectoryInfo directoryInfo)
	{
		while (true)
		{
			if (IsSolutionDirectory(directoryInfo)) return directoryInfo;
			if (directoryInfo.Parent == null) return null;
			directoryInfo = directoryInfo.Parent;
		}
	}

	private FileInfo[] TryFindFluentMigratorProjects(DirectoryInfo rootDirectory)
	{
		var result = new List<FileInfo>();

		foreach (var fileInfo in rootDirectory.GetFiles("*.csproj", SearchOption.AllDirectories))
			if (IsFluentMigratorProject(fileInfo) == true)
				result.Add(fileInfo);

		return result.ToArray();
	}

	#endregion
}
