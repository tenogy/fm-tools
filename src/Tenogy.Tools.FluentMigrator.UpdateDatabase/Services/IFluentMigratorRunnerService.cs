using System;
using System.IO;
using System.Linq;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Conventions;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Logging;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;
using Tenogy.Tools.FluentMigrator.UpdateDatabase.Options;

namespace Tenogy.Tools.FluentMigrator.UpdateDatabase.Services;

public interface IFluentMigratorRunnerService
{
	void Run(FluentMigratorRunnerOptions options);
}

public sealed class FluentMigratorRunnerService : IFluentMigratorRunnerService
{
	private readonly IVersionTableFinderService _versionTableFinderService;

	public static readonly FluentMigratorRunnerService Default = new(null);

	public FluentMigratorRunnerService(
		IVersionTableFinderService? versionTableFinderService
	)
	{
		_versionTableFinderService = versionTableFinderService ?? VersionTableFinderService.Default;
	}

	public void Run(FluentMigratorRunnerOptions options)
	{
		ConsoleColored.WriteMutedLine("Trying to send a command to the FluentMigrator...");
		ConsoleLogger.LogDebug("Trying to send a command to the FluentMigrator...");

		var services = GetServiceCollection(options);

		if (options.Verbose)
			services.AddSingleton<ILoggerProvider, FluentMigratorConsoleLoggerProvider>();

		if (options.Output)
			services.Configure<LogFileFluentMigratorLoggerOptions>(opt =>
				{
					opt.ShowSql = true;
					opt.OutputFileName = options.OutputFileName;
					opt.OutputGoBetweenStatements = options.ProcessorType.StartsWith("SqlServer", StringComparison.InvariantCultureIgnoreCase);
					opt.OutputSemicolonDelimiter = options.ProcessorType.StartsWith("Sqlite", StringComparison.InvariantCultureIgnoreCase);
				})
				.AddSingleton<ILoggerProvider, LogFileFluentMigratorLoggerProvider>();

		using var serviceProvider = services.BuildServiceProvider(validateScopes: false);
		var executor = serviceProvider.GetRequiredService<TaskExecutor>();
		executor.Execute();

		ConsoleColored.WriteInfoLine("The FluentMigrator successfully processed the command.");
		ConsoleLogger.LogInformation("The FluentMigrator successfully processed the command");
	}

	private IServiceCollection GetServiceCollection(FluentMigratorRunnerOptions options)
		=> new ServiceCollection()
			.AddFluentMigratorCore()
			.ConfigureRunner(x => x
				.AddPostgres()
				.AddPostgres92()
				.AddSQLite()
				.AddSqlServer()
				.AddSqlServer2000()
				.AddSqlServer2005()
				.AddSqlServer2008()
				.AddSqlServer2012()
				.AddSqlServer2014()
				.AddSqlServer2016()
				.AddSqlServerCe()
				.WithVersionTable(_versionTableFinderService.Search(new FileInfo(options.TargetAssembly)))
				.WithGlobalCommandTimeout(TimeSpan.FromHours(1))
			)
			.Configure<FluentMigratorLoggerOptions>(opt =>
			{
				opt.ShowElapsedTime = options.Verbose;
				opt.ShowSql = options.Verbose;
			})
			.AddSingleton<IConventionSet>(new DefaultConventionSet(options.DefaultSchemaName, options.WorkingDirectory))
			.Configure<SelectingProcessorAccessorOptions>(opt => opt.ProcessorId = options.ProcessorType)
			.Configure<AssemblySourceOptions>(opt => opt.AssemblyNames = new[] { options.TargetAssembly })
			.Configure<TypeFilterOptions>(opt =>
			{
				opt.Namespace = options.NameSpace;
				opt.NestedNamespaces = options.NestedNameSpaces;
			})
			.Configure<RunnerOptions>(opt =>
			{
				opt.Task = options.Task;
				opt.Version = options.Version;
				opt.StartVersion = options.StartVersion;
				opt.NoConnection = options.NoConnection;
				opt.Steps = options.Steps;
				opt.Profile = options.Profile;
				opt.Tags = options.Tags?.ToArray() ?? Array.Empty<string>();
				opt.TransactionPerSession = options.TransactionPerSession;
				opt.AllowBreakingChange = options.AllowBreakingChange;
			})
			.Configure<ProcessorOptions>(opt =>
			{
				opt.ConnectionString = options.ConnectionString;
				opt.PreviewOnly = options.PreviewOnly;
				opt.ProviderSwitches = options.ProviderSwitches;
				opt.StripComments = options.StripComments;
				opt.Timeout = options.Timeout == null ? null : TimeSpan.FromSeconds(options.Timeout.Value);
			});
}
