using System;
using System.Threading.Tasks;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.UpdateDatabase;

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
			ConsoleLogger.LogInformation("Start UpdateDatabase...");

			if (_arguments.Script)
				await UpdateDatabaseTool.Default.UpdateAndOpen(_arguments.AssemblyPath, _arguments.ProcessorType, _arguments.ConnectionString);
			else
				await UpdateDatabaseTool.Default.Update(_arguments.AssemblyPath, _arguments.ProcessorType, _arguments.ConnectionString);

			ConsoleLogger.LogInformation("Stop UpdateDatabase");
		}
		catch (Exception e)
		{
			if (ConsoleColored.LastForegroundColor != ConsoleColor.Red) ConsoleColored.WriteDangerLine(e.Message);
			ConsoleLogger.LogError(e, e.Message);
		}
	}
}
