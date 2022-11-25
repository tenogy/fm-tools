using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace Tenogy.Tools.FluentMigrator.UpdateDatabase.Console;

internal sealed class Arguments
{
	[Option('s', "script", HelpText = "Generate an SQL file without updating the database.")]
	public bool Script { get; set; }

	[Option('a', "assembly", HelpText = "The path to the assembled FluentMigrator project.")]
	public string? AssemblyPath { get; set; }

	[Option('c', "connection-string", HelpText = "Database connection string or key of ConnectionStrings section in appsettings.json.")]
	public string? ConnectionString { get; set; }

	[Option('d', "database", HelpText = "The type of database. Available options: PostgreSql, SqlServer, SqlLite.")]
	public string? ProcessorType { get; set; }

	[Option('v', "verbose", HelpText = "Enable logging.")]
	public bool Verbose { get; set; }

	[Option("environment", Hidden = true)]
	public bool Environment { get; set; }

	public static Arguments? Create(IEnumerable<string> args)
	{
		var parserResult = Parser.Default.ParseArguments<Arguments>(args);
		return parserResult?.Errors?.Any() == true ? null : parserResult?.Value;
	}
}
