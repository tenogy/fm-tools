using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator;
using Tenogy.Tools.FluentMigrator.AddMigration.Console;
using Tenogy.Tools.FluentMigrator.Services;

var arguments = Arguments.Create(args);

if (arguments == null)
	return;

await Host.CreateDefaultBuilder(args).ConfigureAppConfiguration((context, builder) =>
{
	builder.AddJsonFile("appsettings.json", optional: true);
	builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
	builder.AddEnvironmentVariables();
}).ConfigureServices(services =>
{
	// Prepare
	services.AddLogging();

	// Options
	services.AddSingleton(arguments);

	// Services
	services.AddSingleton<IDatabaseProcessorTypeService, DatabaseProcessorTypeService>();
	services.AddSingleton<IOpenFileService, OpenFileService>();
	services.AddSingleton<IProcessRunnerService, ProcessRunnerService>();
	services.AddSingleton<IProjectAppSettingsService, ProjectAppSettingsService>();
	services.AddSingleton<IProjectAssemblySearchService, ProjectAssemblySearchService>();
	services.AddSingleton<IProjectBuilderService, ProjectBuilderService>();
	services.AddSingleton<IProjectSearchService, ProjectSearchService>();

	// Tool
	services.AddSingleton<IAddMigrationTool, AddMigrationTool>();

	// Worker
	services.AddSingleton<Worker>();
}).ConfigureLogging(builder =>
{
	if (!arguments.Verbose)
		builder.ClearProviders();
}).Build().Services.GetService<Worker>()!.DoWork();
