using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.AddMigration.Console;

internal sealed class Worker
{
	private readonly ILogger<Worker> _logger;
	private readonly Arguments _arguments;
	private readonly IAddMigrationTool _addMigrationTool;

	public Worker(
		ILogger<Worker> logger,
		Arguments arguments,
		IAddMigrationTool addMigrationTool
	)
	{
		_logger = logger;
		_arguments = arguments;
		_addMigrationTool = addMigrationTool;
	}

	public async Task DoWork()
	{
		try
		{
			_logger.LogInformation("Start AddMigration...");

			if (string.IsNullOrWhiteSpace(_arguments.MigrationName))
				throw new InvalidOperationException("Migration name is empty");

			if (_arguments.Silent)
				await _addMigrationTool.Add(_arguments.MigrationName!);
			else
				await _addMigrationTool.AddAndOpen(_arguments.MigrationName!);

			_logger.LogInformation("Stop AddMigration");
		}
		catch (Exception e)
		{
			if (ConsoleColored.LastForegroundColor != ConsoleColor.Red) ConsoleColored.WriteDangerLine(e.Message);
			_logger.LogCritical(e, e.Message);
		}
	}
}
