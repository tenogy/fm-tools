using FluentMigrator.Runner.VersionTableInfo;

namespace MigrationsProjectNamespace;

public class VersionTable : IVersionTableMetaData
{
	public object? ApplicationContext { get; set; }

	public virtual string SchemaName => string.Empty;

	public virtual string TableName => "__versions";

	public virtual string ColumnName => "Version";

	public virtual string UniqueIndexName => "UC_Version";

	public virtual string AppliedOnColumnName => "AppliedOn";

	public virtual string DescriptionColumnName => "Description";

	public virtual bool OwnsSchema => true;
}
