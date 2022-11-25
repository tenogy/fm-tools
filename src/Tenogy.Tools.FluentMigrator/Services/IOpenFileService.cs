using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IOpenFileService
{
	Task Open(FileInfo fileInfo);
}

public sealed class OpenFileService : IOpenFileService
{
	private readonly IProcessRunnerService _processRunnerService;
	private readonly ILogger<OpenFileService>? _logger;

	public static readonly OpenFileService Default = new(null, null);

	public OpenFileService(
		ILogger<OpenFileService>? logger,
		IProcessRunnerService? processRunner
	)
	{
		_logger = logger;
		_processRunnerService = processRunner ?? ProcessRunnerService.Default;
	}

	public async Task Open(FileInfo fileInfo)
	{
		_logger?.LogDebug("Trying open a file '{FileToOpenPath}'...", fileInfo.FullName);

		var filePath = fileInfo.FullName;
		var arguments = '"' + filePath.Replace("\"", "\\\"") + '"';

		if (!File.Exists(filePath))
		{
			ConsoleColored.WriteDangerLine("The file does not exist.");
			throw new FileNotFoundException("The file does not exist11.", filePath);
		}

		if (Environment.OSVersion.Platform == PlatformID.Unix)
		{
			ConsoleColored.WriteMutedLine($"Opening the file: {fileInfo.Name}");
			await _processRunnerService.Run("open", arguments);
			return;
		}

		var ide = GetIde();

		switch (ide)
		{
			case EnumIde.VisualStudio:
				ConsoleColored.WriteMutedLine($"Opening the file in Visual Studio: {fileInfo.Name}");
				await _processRunnerService.Run("devenv", $"/edit {arguments}", true);
				break;
			case EnumIde.VisualStudioCode:
				ConsoleColored.WriteMutedLine($"Opening the file in Visual Studio Code: {fileInfo.Name}");
				await _processRunnerService.Run("Code", arguments, true);
				break;
			case EnumIde.Rider:
			{
				var rider64Path = GetRider64Path();

				if (string.IsNullOrWhiteSpace(rider64Path)) goto case EnumIde.Unknown;

				ConsoleColored.WriteMutedLine($"Opening the file in Rider: {fileInfo.Name}");
				await _processRunnerService.Run(rider64Path!, arguments, true);
			}
				break;
			case EnumIde.Unknown:
			default:
				ConsoleColored.WriteMutedLine($"Opening the file: {fileInfo.Name}");
				await _processRunnerService.Run(arguments, null, true);
				break;
		}
	}

	#region Helpers

	private enum EnumIde
	{
		Unknown,
		VisualStudio,
		VisualStudioCode,
		Rider
	}

	private EnumIde GetIde()
	{
		_logger?.LogDebug("Trying to determine which IDE the tool was launched...");

		var process = Process.GetCurrentProcess();

		while (process != null)
		{
			switch (process.ProcessName)
			{
				case "devenv": return EnumIde.VisualStudio;
				case "Code": return EnumIde.VisualStudioCode;
				case "rider64": return EnumIde.Rider;
			}

			try
			{
				process = ParentProcessFinder.Find(process);
			}
			catch (Exception e)
			{
				_logger?.LogError(e, "Failed determine which IDE the tool was running in.");
				break;
			}
		}

		return EnumIde.Unknown;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct ParentProcessFinder
	{
		private readonly IntPtr Reserved1;
		private readonly IntPtr PebBaseAddress;
		private readonly IntPtr Reserved2_0;
		private readonly IntPtr Reserved2_1;
		private readonly IntPtr UniqueProcessId;
		private readonly IntPtr InheritedFromUniqueProcessId;

		[DllImport("ntdll.dll")]
		private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessFinder processInformation, int processInformationLength, out int returnLength);

		public static Process? Find(Process process)
			=> Find(process.Handle);

		private static Process? Find(IntPtr handle)
		{
			var pbi = new ParentProcessFinder();
			var status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
			if (status != 0) return null;

			try
			{
				return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
	}

	private static string? GetRider64Path()
	{
		var rider64Path = (Environment.GetEnvironmentVariable("PATH") ?? "")
			.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => Path.Combine(x.Trim(), "rider64.exe"))
			.Where(File.Exists)
			.FirstOrDefault() ?? GetFromProgramFiles();

		return string.IsNullOrWhiteSpace(rider64Path) ? null : '"' + rider64Path + '"';

		string? GetFromProgramFiles()
		{
			var programFiles = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "JetBrains"));
			if (!programFiles.Exists) return null;

			var version = programFiles.GetDirectories("JetBrains Rider*").OrderByDescending(x => x.Name).FirstOrDefault();
			if (version == null) return null;

			var result = Path.Combine(version.FullName, "bin", "rider64.exe");

			return File.Exists(result) ? result : null;
		}
	}

	#endregion
}
