using System.Linq;
using System.Text.RegularExpressions;

namespace Tenogy.Tools.FluentMigrator.AddMigration;

internal interface IMigrationUpTemplate
{
	string? GetUp(string migrationName);
}

internal sealed class CreateTableTemplate : MigrationUpTemplateBase, IMigrationUpTemplate
{
	public static readonly IMigrationUpTemplate Default = new CreateTableTemplate();

	protected override Regex Regex => new("^CreateTable(\\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	protected override string ParseMatch(Match match)
		=> $@"
Create.Table(""{GetSnakeCase(match.Groups[1].Value)}"")
	.WithColumn(""id"").AsGuid().NotNullable().PrimaryKey();
".Trim();
}

internal sealed class AlterTableTemplate : MigrationUpTemplateBase
{
	public static readonly IMigrationUpTemplate Default = new AlterTableTemplate();

	protected override Regex Regex => new("^AlterTable(\\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	protected override string ParseMatch(Match match)
		=> $@"
Alter.Table(""{GetSnakeCase(match.Groups[1].Value)}"")
	.AddColumn(""column_name"").AsBoolean().NotNullable().WithDefaultValue(false);
".Trim();
}

internal abstract class MigrationUpTemplateBase : IMigrationUpTemplate
{
	protected abstract Regex Regex { get; }

	public string? GetUp(string migrationName)
	{
		var match = Regex.Match(migrationName);

		if (!match.Success) return null;

		return ParseMatch(match);
	}

	protected abstract string? ParseMatch(Match match);

	protected static string GetSnakeCase(string value)
	{
		if (string.IsNullOrWhiteSpace(value)) return "";

		return string.Join("", value.Select((x, i) =>
		{
			if (i == 0) return char.ToLowerInvariant(x).ToString();
			if (char.IsUpper(x)) return "_" + char.ToLowerInvariant(x);
			return x.ToString();
		}));
	}
}
