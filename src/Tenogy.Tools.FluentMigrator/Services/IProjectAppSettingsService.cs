using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IProjectAppSettingsService
{
	FileInfo Search(string projectAssemblyDirectoryPath);

	Task<Dictionary<string, string>> GetConnectionStrings(string projectAssemblyDirectoryPath);

	Task<string> GetConnectionString(string projectAssemblyDirectoryPath, string? connectionStringOrKey);
}

public sealed class ProjectAppSettingsService : IProjectAppSettingsService
{
	private readonly ILogger<ProjectAppSettingsService>? _logger;

	public static readonly ProjectAppSettingsService Default = new ProjectAppSettingsService(null);

	public ProjectAppSettingsService(
		ILogger<ProjectAppSettingsService>? logger
	)
	{
		_logger = logger;
	}

	public FileInfo Search(string projectAssemblyDirectoryPath)
	{
		_logger?.LogDebug("Trying to find `appsettings.Development.json` or `appsettings.json` file...");

		var result = SearchAppSettings(projectAssemblyDirectoryPath);

		if (result?.Exists != true)
		{
			_logger?.LogWarning("The appsettings.json file was not found");
			throw new FileNotFoundException("The appsettings.json file was not found.");
		}

		_logger?.LogInformation("The '{AppSettingsPath}' file found", result.FullName);
		return result;
	}

	public async Task<Dictionary<string, string>> GetConnectionStrings(string projectAssemblyDirectoryPath)
	{
		var fileInfo = SearchAppSettings(projectAssemblyDirectoryPath);

		if (fileInfo == null)
			return new Dictionary<string, string>();

		_logger?.LogDebug("Trying to deserialize `{AppSettingsFile}` file...", fileInfo.Name);

		try
		{
			var content = await Task.Run(() => File.ReadAllText(fileInfo.FullName));
			var result = JsonNode.Parse(content)?["ConnectionStrings"].Deserialize<Dictionary<string, string>>() ?? new Dictionary<string, string>();
			_logger?.LogInformation("The file `{AppSettingsFile}` was successfully deserialized", fileInfo.Name);
			return result;
		}
		catch (Exception e)
		{
			_logger?.LogError(e, "Failed to deserialize the file `{AppSettingsFile}`", fileInfo.Name);
			return new Dictionary<string, string>();
		}
	}

	public async Task<string> GetConnectionString(string projectAssemblyDirectoryPath, string? connectionStringOrKey = null)
	{
		var connectionStrings = await GetConnectionStrings(projectAssemblyDirectoryPath);

		ConsoleColored.WriteMutedLine("Trying to find the connection string to the database...");
		_logger?.LogDebug("Trying to find the connection string to the database from appsettings...");

		if (!string.IsNullOrWhiteSpace(connectionStringOrKey))
		{
			_logger?.LogDebug("The connection string '{ConnectionStringsValue}' to the database was passed to the tool.", connectionStringOrKey);

			if (connectionStrings.ContainsKey(connectionStringOrKey!))
			{
				var result = connectionStrings[connectionStringOrKey!];
				ConsoleColored.WriteInfoLine($"Database connection string: {result}");
				_logger?.LogInformation("The AppSettings file contains connection string with key `{ConnectionStringsKey}` and value ``{ConnectionStringsValue}``", connectionStringOrKey, result);
				return result;
			}

			ConsoleColored.WriteInfoLine($"Database connection string: {connectionStringOrKey}");
			_logger?.LogInformation("Use the connection string from the passed to the tool flag: {ConnectionStringsValue}", connectionStringOrKey);
			return connectionStringOrKey!;
		}

		if (connectionStrings.Any() != true)
		{
			ConsoleColored.WriteDangerLine("In the file appsettings database connection strings not found.");
			throw new InvalidOperationException("In the file appsettings database connection strings not found.");
		}

		if (connectionStrings.Keys.Count > 1)
		{
			var keys = string.Join(", ", connectionStrings.Keys.Select(x => "'" + x + "'"));
			ConsoleColored.WriteDangerLine($"In the file appsettings more than one database connection strings found: {keys}. You can pass the --connection-string flag containing the key of the database connection string.");
			throw new InvalidOperationException($"In the file appsettings more than one database connection strings found: {keys}");
		}

		var key = connectionStrings.Keys.First();
		var value = connectionStrings[key];

		ConsoleColored.WriteInfoLine($"Database connection string: {value}");
		_logger?.LogInformation("Use AppSettings connection string with key `{ConnectionStringsKey}` and value ``{ConnectionStringsValue}``", key, value);

		return value;
	}

	private static FileInfo? SearchAppSettings(string projectAssemblyDirectoryPath)
	{
		var result = new FileInfo(Path.Combine(projectAssemblyDirectoryPath, "appsettings.Development.json"));

		if (!result.Exists)
			result = new FileInfo(Path.Combine(projectAssemblyDirectoryPath, "appsettings.json"));

		return !result.Exists ? null : result;
	}
}
