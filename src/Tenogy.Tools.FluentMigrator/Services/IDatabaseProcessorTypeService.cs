using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IDatabaseProcessorTypeService
{
	string Get(string connectionString, string? processorType);
}

public sealed class DatabaseProcessorTypeService : IDatabaseProcessorTypeService
{
	private readonly ILogger<DatabaseProcessorTypeService>? _logger;

	public static readonly DatabaseProcessorTypeService Default = new(null);

	public DatabaseProcessorTypeService(
		ILogger<DatabaseProcessorTypeService>? logger
	)
	{
		_logger = logger;
	}

	public string Get(string connectionString, string? processorType)
	{
		ConsoleColored.WriteMutedLine("Trying to determine the type of database...");
		_logger?.LogDebug("Trying to determine the type of database ({ProcessorType})...", processorType);

		var result = !string.IsNullOrWhiteSpace(processorType)
			? GetProcessorTypeFromOptions(processorType!)
			: GetProcessorTypeFromConnectionString(connectionString);

		if (string.IsNullOrWhiteSpace(result))
		{
			ConsoleColored.WriteDangerLine("The database type could not be determined.");
			throw new InvalidOperationException($"The database type '{processorType}' could not be determined.");
		}

		ConsoleColored.WriteInfoLine($"Database type: {result}");
		_logger?.LogInformation("The database type '{ProcessorType}' has been successfully determined", result);
		return result!;
	}

	#region Helpers

	private static string? GetProcessorTypeFromOptions(string processorType)
	{
		if (new[] { "pg", "postgres", "postgresql" }.Contains(processorType, StringComparer.OrdinalIgnoreCase))
			return "Postgres";

		if (new[] { "sqlite" }.Contains(processorType, StringComparer.OrdinalIgnoreCase))
			return "Sqlite";

		return processorType.ToLower(CultureInfo.InvariantCulture) switch
		{
			// PostgreSQL
			"pg" => "Postgres",
			"postgres" => "Postgres",
			"postgresql" => "Postgres",
			// SQLite
			"sqlite" => "Sqlite",
			// SQLServer
			"sqlserver2000" => "SqlServer2000",
			"sqlserver2005" => "SqlServer2005",
			"sqlserver2008" => "SqlServer2008",
			"sqlserver2012" => "SqlServer2012",
			"sqlserver2014" => "SqlServer2014",
			"sqlserver2016" => "SqlServer2016",
			"sqlserver2017" => "SqlServer2016",
			"sqlserver2019" => "SqlServer2016",
			"sqlserver" => "SqlServer",
			// Unknown
			_ => null
		};
	}

	private static string? GetProcessorTypeFromConnectionString(string connectionString)
	{
		var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

		if (ExistsPart("Server=") && ExistsPart("InitialCatalog="))
			return "SqlServer2016";

		if (ExistsPart("Host=") && ExistsPart("Database="))
			return "Postgres";

		if (ExistsPart("DataSource="))
			return "Sqlite";

		return null;

		bool ExistsPart(string partStartWith)
			=> parts.Any(x => new Regex(@"\s+", RegexOptions.Compiled).Replace(x, "").StartsWith(partStartWith, StringComparison.OrdinalIgnoreCase));
	}

	#endregion
}
