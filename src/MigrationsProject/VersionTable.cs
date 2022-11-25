using FluentMigrator.Runner.VersionTableInfo;

namespace MigrationsProjectNamespace;

public class VersionTable : IVersionTableMetaData
{
	public object? ApplicationContext { get; set; }
	public string AppliedOnColumnName => "applied_on";
	public string ColumnName => "version";
	public string DescriptionColumnName => "migration";
	public bool OwnsSchema => false;
	public string SchemaName => "";
	public string TableName => "_version";
	public string UniqueIndexName => "uc_version";
}
