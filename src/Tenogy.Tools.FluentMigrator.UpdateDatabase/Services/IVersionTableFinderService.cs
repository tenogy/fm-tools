using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Tenogy.Tools.FluentMigrator.Services;

public interface IVersionTableFinderService
{
	IVersionTableMetaData? Search(FileInfo assemblyFileInfo);
}

public sealed class VersionTableFinderService : IVersionTableFinderService
{
	private readonly ILogger<VersionTableFinderService>? _logger;

	public static readonly VersionTableFinderService Default = new(null);

	public VersionTableFinderService(
		ILogger<VersionTableFinderService>? logger
	)
	{
		_logger = logger;
	}

	public IVersionTableMetaData? Search(FileInfo assemblyFileInfo)
		=> FindInAssembly(Assembly.LoadFrom(assemblyFileInfo.FullName)) ?? TryFindInTenogyAppFluentMigrator(assemblyFileInfo.Directory!);

	private IVersionTableMetaData? FindInAssembly(Assembly assembly)
	{
		_logger?.LogDebug("Trying to find IVersionTableMetaData in assembly '{Assembly}'...", assembly.FullName);

		var versionTableMetaDataType = typeof(IVersionTableMetaData);
		var type = assembly.GetTypes().FirstOrDefault(versionTableMetaDataType.IsAssignableFrom);

		if (type == null)
		{
			_logger?.LogDebug("IVersionTableMetaData not found in assembly '{Assembly}'", assembly.FullName);
			return null;
		}

		_logger?.LogDebug("Creating instance of {IVersionTableMetaData}...", type.FullName);
		return Activator.CreateInstance(type) as IVersionTableMetaData;
	}

	private IVersionTableMetaData? TryFindInTenogyAppFluentMigrator(FileSystemInfo directoryInfo)
	{
		var assemblyFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "Tenogy.App.FluentMigrator.dll"));
		return !assemblyFileInfo.Exists ? null : FindInAssembly(Assembly.LoadFrom(assemblyFileInfo.FullName));
	}
}
