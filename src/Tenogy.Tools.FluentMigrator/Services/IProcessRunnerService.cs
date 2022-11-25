using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tenogy.Tools.FluentMigrator.Services;

public interface IProcessRunnerService
{
	Task Run(string exe, string? arguments = null, bool useShellExecute = false);

	Task Run(ProcessStartInfo processStartInfo);

	Task<(int exitCode, string? output)> RunAndWait(string exe, string? arguments = null, bool useShellExecute = false);

	Task<(int exitCode, string? output)> RunAndWait(ProcessStartInfo processStartInfo);
}

public sealed class ProcessRunnerService : IProcessRunnerService
{
	private readonly ILogger<ProcessRunnerService>? _logger;

	public static readonly ProcessRunnerService Default = new(null);

	public ProcessRunnerService(
		ILogger<ProcessRunnerService>? logger
	)
	{
		_logger = logger;
	}

	private static ProcessStartInfo GetDefaultProcessStartInfo(string exe, string? arguments = null, bool useShellExecute = false)
		=> new()
		{
			FileName = exe,
			Arguments = arguments,
			WindowStyle = ProcessWindowStyle.Hidden,
			RedirectStandardInput = false,
			RedirectStandardOutput = !useShellExecute,
			CreateNoWindow = true,
			UseShellExecute = useShellExecute,
		};

	public async Task Run(string exe, string? arguments = null, bool useShellExecute = false)
		=> await Run(GetDefaultProcessStartInfo(exe, arguments, useShellExecute));

	public async Task Run(ProcessStartInfo processStartInfo)
	{
		var (process, processCommand) = CreateProcess(processStartInfo);

		_logger?.LogDebug("Run process: {ProcessCommand}", processCommand);

		await Task.Run(() =>
		{
			process.Start();
			process.Dispose();
		});
	}

	public async Task<(int exitCode, string? output)> RunAndWait(string exe, string? arguments = null, bool useShellExecute = false)
		=> await RunAndWait(GetDefaultProcessStartInfo(exe, arguments, useShellExecute));

	public async Task<(int exitCode, string? output)> RunAndWait(ProcessStartInfo processStartInfo)
	{
		var (process, processCommand) = CreateProcess(processStartInfo);

		_logger?.LogDebug("Run and wait process: {ProcessCommand}", processCommand);

		var (exitCode, output) = await RunProcessAndWait(process);
		process.Dispose();

		_logger?.LogInformation("Process exit code: {ProcessExitCode}. Process command: {ProcessCommand}", exitCode, processCommand);

		return (exitCode, output);
	}

	#region Helpers

	private static (Process process, string processCommand) CreateProcess(ProcessStartInfo processStartInfo)
	{
		var processCommand = (processStartInfo.FileName + " " + processStartInfo.Arguments).Trim();
		return (new Process { StartInfo = processStartInfo }, processCommand);
	}

	private static async Task<(int exitCode, string? output)> RunProcessAndWait(Process process)
	{
		try
		{
			process.Start();
			process.WaitForExit();
			return (process.ExitCode, process.ExitCode == 0 ? null : await process.StandardOutput.ReadToEndAsync());
		}
		catch (Exception exp)
		{
			return (1, exp.Message);
		}
	}

	#endregion
}
