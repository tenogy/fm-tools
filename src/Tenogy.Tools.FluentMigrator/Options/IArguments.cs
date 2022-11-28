namespace Tenogy.Tools.FluentMigrator.Options;

public interface IArguments
{
	bool Verbose { get; set; }

	string? Environment { get; set; }
}
