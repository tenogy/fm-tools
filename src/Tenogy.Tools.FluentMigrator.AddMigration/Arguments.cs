using System.Collections.Generic;
using System.Linq;
using CommandLine;
using Tenogy.Tools.FluentMigrator.Options;

namespace Tenogy.Tools.FluentMigrator.AddMigration;

internal sealed class Arguments : IArguments
{
	[Value(0, MetaName = "Migration name", Required = true, HelpText = "The name of your migration.")]
	public string? MigrationName { get; set; }

	[Option('s', "silent", HelpText = "Do not open the migration file after creation.")]
	public bool Silent { get; set; }

	[Option('v', "verbose", HelpText = "Enable logging.")]
	public bool Verbose { get; set; }

	[Option("environment", Hidden = true)]
	public string? Environment { get; set; }

	public static Arguments? Create(IEnumerable<string> args)
	{
		var parserResult = Parser.Default.ParseArguments<Arguments>(args);
		return parserResult?.Errors?.Any() == true ? null : parserResult?.Value;
	}
}
