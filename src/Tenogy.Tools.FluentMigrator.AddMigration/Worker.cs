using System;
using System.Threading.Tasks;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.AddMigration;

internal sealed class Worker
{
	private readonly Arguments? _arguments;

	public Worker(Arguments? arguments)
	{
		_arguments = arguments;
	}

	public async Task DoWork()
	{
		if (_arguments == null)
			return;

		ConsoleLogger.Available = _arguments.Verbose;

		try
		{
			ConsoleLogger.LogInformation("Start AddMigration...");

			if (string.IsNullOrWhiteSpace(_arguments.MigrationName))
				throw new InvalidOperationException("Migration name is empty");

			if (_arguments.Silent)
				await AddMigrationTool.Default.Add(_arguments.MigrationName!);
			else
				await AddMigrationTool.Default.AddAndOpen(_arguments.MigrationName!);

			ConsoleLogger.LogInformation("Stop AddMigration");
		}
		catch (Exception e)
		{
			if (ConsoleColored.LastForegroundColor != ConsoleColor.Red) ConsoleColored.WriteDangerLine(e.Message);
			ConsoleLogger.LogError(e, e.Message);
		}
	}
}
