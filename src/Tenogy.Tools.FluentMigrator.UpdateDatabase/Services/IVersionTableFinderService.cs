using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentMigrator.Runner.VersionTableInfo;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.UpdateDatabase.Services;

internal interface IVersionTableFinderService
{
	IVersionTableMetaData? Search(FileInfo assemblyFileInfo);
}

internal sealed class VersionTableFinderService : IVersionTableFinderService
{
	public static readonly VersionTableFinderService Default = new();

	public IVersionTableMetaData? Search(FileInfo assemblyFileInfo)
		=> FindInAssembly(Assembly.LoadFrom(assemblyFileInfo.FullName)) ?? TryFindInTenogyAppFluentMigrator(assemblyFileInfo.Directory!);

	private IVersionTableMetaData? FindInAssembly(Assembly assembly)
	{
		ConsoleLogger.LogDebug("Trying to find IVersionTableMetaData in assembly '{Assembly}'...", assembly.FullName);

		var versionTableMetaDataType = typeof(IVersionTableMetaData);
		var type = assembly.GetTypes().FirstOrDefault(versionTableMetaDataType.IsAssignableFrom);

		if (type == null)
		{
			ConsoleLogger.LogDebug("IVersionTableMetaData not found in assembly '{Assembly}'", assembly.FullName);
			return null;
		}

		ConsoleLogger.LogDebug("Creating instance of {IVersionTableMetaData}...", type.FullName);
		return Activator.CreateInstance(type) as IVersionTableMetaData;
	}

	private IVersionTableMetaData? TryFindInTenogyAppFluentMigrator(FileSystemInfo directoryInfo)
	{
		var assemblyFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "Tenogy.App.FluentMigrator.dll"));
		return !assemblyFileInfo.Exists ? null : FindInAssembly(Assembly.LoadFrom(assemblyFileInfo.FullName));
	}
}
