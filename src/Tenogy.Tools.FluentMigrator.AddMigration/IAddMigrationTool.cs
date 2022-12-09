using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Tenogy.Tools.FluentMigrator.Helpers;
using Tenogy.Tools.FluentMigrator.Services;

namespace Tenogy.Tools.FluentMigrator.AddMigration;

internal interface IAddMigrationTool
{
	Task<FileInfo> Add(string migrationName);

	Task<FileInfo> AddAndOpen(string migrationName);
}

internal sealed partial class AddMigrationTool : IAddMigrationTool
{
	private readonly IProjectSearchService _projectSearchService;
	private readonly IOpenFileService _openFileService;
	private readonly IEnumerable<IMigrationUpTemplate> _migrationUpTemplates;

	[GeneratedRegex("^(?!$)", RegexOptions.Multiline)]
	private static partial Regex RegexLineStart();

	public static readonly AddMigrationTool Default = new(null, null, null);

	public AddMigrationTool(
		IProjectSearchService? projectFinder,
		IOpenFileService? openFile,
		IEnumerable<IMigrationUpTemplate>? migrationUpTemplates
	)
	{
		_projectSearchService = projectFinder ?? ProjectSearchService.Default;
		_openFileService = openFile ?? OpenFileService.Default;
		_migrationUpTemplates = migrationUpTemplates ?? new[]
		{
			CreateTableTemplate.Default,
			AlterTableTemplate.Default
		};
	}

	public async Task<FileInfo> Add(string migrationName)
	{
		var migrationFile = await AddMigration(migrationName);
		ConsoleColored.WriteSuccessLine("Migration creation has been completed successfully.");
		return migrationFile;
	}

	public async Task<FileInfo> AddAndOpen(string migrationName)
	{
		var migrationFile = await AddMigration(migrationName);
		ConsoleLogger.LogDebug("Trying open a file '{MigrationFilePath}'...", migrationFile.FullName);
		await _openFileService.Open(migrationFile);
		ConsoleColored.WriteSuccessLine("Migration creation has been completed successfully.");
		return migrationFile;
	}

	private async Task<FileInfo> AddMigration(string migrationName)
	{
		if (!IsValidMigrationName(migrationName))
		{
			ConsoleColored.WriteDangerLine("Invalid name was passed for the migration file.");
			throw new InvalidOperationException($"Migration file name '{migrationName}' not valid");
		}

		var project = _projectSearchService.Search(Environment.CurrentDirectory);
		var migrationFile = new MigrationFile(migrationName, project);

		ValidateMigrationFile(migrationFile);

		await SaveMigrationFile(migrationFile);

		return migrationFile.FileInfo;
	}

	#region Helpers

	private static bool IsValidMigrationName(string? migrationName)
	{
		var nextMustBeStartChar = true;

		if (string.IsNullOrWhiteSpace(migrationName)) return false;

		foreach (var ch in migrationName!)
		{
			var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
			switch (uc)
			{
				case UnicodeCategory.UppercaseLetter:
				case UnicodeCategory.LowercaseLetter:
				case UnicodeCategory.TitlecaseLetter:
				case UnicodeCategory.ModifierLetter:
				case UnicodeCategory.LetterNumber:
				case UnicodeCategory.OtherLetter:
					nextMustBeStartChar = false;
					break;

				case UnicodeCategory.NonSpacingMark:
				case UnicodeCategory.SpacingCombiningMark:
				case UnicodeCategory.ConnectorPunctuation:
				case UnicodeCategory.DecimalDigitNumber:
					if (nextMustBeStartChar && ch != '_')
						return false;
					nextMustBeStartChar = false;
					break;
				default:
					return false;
			}
		}

		return true;
	}

	private void ValidateMigrationFile(MigrationFile migrationFile)
	{
		ConsoleColored.WriteMutedLine("Trying to create a migration file...");
		ConsoleLogger.LogDebug("Trying to create a migration file '{MigrationFileName}'...", migrationFile.Name);

		if (!migrationFile.FileInfo.Directory!.Exists)
			Directory.CreateDirectory(migrationFile.FileInfo.Directory.FullName);

		var migrationExists =
			File.Exists(migrationFile.FileInfo.FullName) ||
			migrationFile.FileInfo.Directory.GetFiles("*_" + migrationFile.Name + ".cs").Any();

		if (!migrationExists)
			return;

		ConsoleColored.WriteDangerLine("Migration with this name already exists.");
		throw new InvalidOperationException($"Migration with name '{migrationFile.Name}' already exists.");
	}

	private async Task SaveMigrationFile(MigrationFile migrationFile)
	{
		var needBraces = migrationFile.NeedNameSpaceBraces;
		var template = GetUpTemplate(migrationFile.Name);

		var classWrap = $@"
[Migration({migrationFile.Version})]
public class {migrationFile.Name} : Migration
{{
	public override void Up()
	{{{template}
	}}

	public override void Down()
	{{
	}}
}}
".Trim();

		if (needBraces)
			classWrap = RegexLineStart().Replace(classWrap, "\t");

		await Task.Run(() =>
		{
			File.WriteAllText(migrationFile.FileInfo.FullName, $@"
using FluentMigrator;

namespace {migrationFile.NameSpace}.Migrations{(needBraces ? "" : ";")}
{(needBraces ? "{" : "")}
{classWrap}
{(needBraces ? "}" : "")}
".TrimStart());
		});

		ConsoleColored.WriteInfoLine("The migration file was created successfully.");
		ConsoleLogger.LogInformation("The migration file was created in the directory '{MigrationFilePath}'", migrationFile.FileInfo.FullName);
	}

	private sealed class MigrationFile
	{
		public string NameSpace { get; }

		public bool NeedNameSpaceBraces { get; }

		public string Version { get; }

		public string Name { get; }

		public FileInfo FileInfo { get; }

		public MigrationFile(string name, FileInfo project)
		{
			var doc = new XmlDocument();
			doc.Load(project.FullName);

			NameSpace = GetNameSpace() ?? Path.GetFileNameWithoutExtension(project.Name);
			NeedNameSpaceBraces = GetNeedNameSpaceBraces() ?? false;
			Version = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			Name = name;
			FileInfo = new FileInfo(Path.Combine(
				project.Directory!.FullName,
				"Migrations",
				$"{Version}_{Name}.cs"
			));

			string? GetNameSpace()
			{
				try
				{
					var rootNameSpace = doc.SelectSingleNode("/Project/PropertyGroup/RootNamespace")?.InnerText;
					return string.IsNullOrWhiteSpace(rootNameSpace) ? null : rootNameSpace;
				}
				catch
				{
					return null;
				}
			}

			bool? GetNeedNameSpaceBraces()
			{
				try
				{
					var langVersion = doc.SelectSingleNode("/Project/PropertyGroup/LangVersion")?.InnerText;
					if (string.IsNullOrWhiteSpace(langVersion)) return false;
					return Convert.ToDouble(langVersion, CultureInfo.InvariantCulture) < 10;
				}
				catch
				{
					return null;
				}
			}
		}
	}

	#endregion

	#region Templates

	private string? GetUpTemplate(string migrationName)
	{
		if (string.IsNullOrWhiteSpace(migrationName)) return null;

		string? result = null;

		foreach (var template in _migrationUpTemplates)
		{
			result = template.GetUp(migrationName);

			if (!string.IsNullOrWhiteSpace(result))
				break;
		}

		if (string.IsNullOrWhiteSpace(result))
			return null;

		return "\n" + RegexLineStart().Replace(result, "\t\t");
	}

	#endregion
}
