using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.UpdateDatabase.Console;

internal sealed class Worker
{
	private readonly ILogger<Worker> _logger;
	private readonly Arguments _arguments;
	private readonly IUpdateDatabaseTool _updateDatabaseTool;

	public Worker(
		ILogger<Worker> logger,
		Arguments arguments,
		IUpdateDatabaseTool updateDatabaseTool
	)
	{
		_logger = logger;
		_arguments = arguments;
		_updateDatabaseTool = updateDatabaseTool;
	}

	public async Task DoWork()
	{
		try
		{
			_logger.LogInformation("Start UpdateDatabase...");

			if (_arguments.Script)
				await _updateDatabaseTool.UpdateAndOpen(_arguments.AssemblyPath, _arguments.ProcessorType, _arguments.ConnectionString);
			else
				await _updateDatabaseTool.Update(_arguments.AssemblyPath, _arguments.ProcessorType, _arguments.ConnectionString);

			_logger.LogInformation("Stop UpdateDatabase");
		}
		catch (Exception e)
		{
			if (ConsoleColored.LastForegroundColor != ConsoleColor.Red) ConsoleColored.WriteDangerLine(e.Message);
			_logger.LogCritical(e, e.Message);
		}
	}
}
