using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tenogy.Tools.FluentMigrator.Helpers;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IOpenFileService
{
	Task Open(FileInfo fileInfo);
}

public sealed class OpenFileService : IOpenFileService
{
	private readonly IProcessRunnerService _processRunnerService;

	public static readonly OpenFileService Default = new(null);

	public OpenFileService(
		IProcessRunnerService? processRunner
	)
	{
		_processRunnerService = processRunner ?? ProcessRunnerService.Default;
	}

	public async Task Open(FileInfo fileInfo)
	{
		ConsoleLogger.LogDebug("Trying open a file '{FileToOpenPath}'...", fileInfo.FullName);

		var filePath = fileInfo.FullName;
		var arguments = '"' + filePath.Replace("\"", "\\\"") + '"';

		if (!File.Exists(filePath))
		{
			ConsoleColored.WriteDangerLine("The file does not exist.");
			throw new FileNotFoundException("The file does not exist.", filePath);
		}

		if (Environment.OSVersion.Platform == PlatformID.Unix)
		{
			var riderPath = GetMacOsRiderPath();

			if (!string.IsNullOrWhiteSpace(riderPath))
			{
				ConsoleColored.WriteMutedLine($"Opening the file in Rider: {fileInfo.Name}");
				await _processRunnerService.Run(riderPath, arguments);
				return;
			}

			ConsoleColored.WriteMutedLine($"Opening the file in Rider: {fileInfo.Name}");
			await _processRunnerService.Run("open", arguments, true);

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
				var riderPath = GetWindowsRiderPath();

				if (string.IsNullOrWhiteSpace(riderPath))
					goto case EnumIde.Unknown;

				ConsoleColored.WriteMutedLine($"Opening the file in Rider: {fileInfo.Name}");
				await _processRunnerService.Run(riderPath, arguments, true);
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
		ConsoleLogger.LogDebug("Trying to determine which IDE the tool was launched...");

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
				ConsoleLogger.LogError(e, "Failed determine which IDE the tool was running in.");
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

	private static string? GetWindowsRiderPath()
	{
		const string fileName = "rider64.exe";
		return GetFromPathEnvironment(fileName, true) ?? GetFromProgramFiles();

		string? GetFromProgramFiles()
		{
			var programFiles = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "JetBrains"));
			if (!programFiles.Exists) return null;

			var version = programFiles
				.GetDirectories("JetBrains Rider*")
				.MaxBy(x => x.Name);

			if (version == null)
				return null;

			var result = Path.Combine(version.FullName, "bin", fileName);

			return File.Exists(result) ? '"' + result + '"' : null;
		}
	}

	private static string? GetMacOsRiderPath()
		=> GetFromPathEnvironment("rider", false);

	private static string? GetFromPathEnvironment(string fileName, bool withQuotes)
	{
		var path = (Environment.GetEnvironmentVariable("PATH") ?? "")
			.Split(new[] { ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => Path.Combine(x.Trim(), fileName))
			.Where(File.Exists)
			.FirstOrDefault();

		if (string.IsNullOrWhiteSpace(path))
			return null;

		return withQuotes ? '"' + path + '"' : path;
	}

	#endregion
}
