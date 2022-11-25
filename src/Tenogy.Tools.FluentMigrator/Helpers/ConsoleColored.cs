using System;

namespace Tenogy.Tools.FluentMigrator.Helpers;

public static class ConsoleColored
{
	public static ConsoleColor? LastForegroundColor { get; private set; }

	public static void WriteMuted(string? message, params object?[] args) => Write(ConsoleColor.Gray, message, args);
	public static void WriteMutedLine(string? message, params object?[] args) => WriteLine(ConsoleColor.Gray, message, args);

	public static void WriteInfo(string? message, params object?[] args) => Write(ConsoleColor.Blue, message, args);
	public static void WriteInfoLine(string? message, params object?[] args) => WriteLine(ConsoleColor.Blue, message, args);

	public static void WriteDanger(string? message, params object?[] args) => Write(ConsoleColor.Red, message, args);
	public static void WriteDangerLine(string? message, params object?[] args) => WriteLine(ConsoleColor.Red, message, args);

	public static void WriteSuccess(string? message, params object?[] args) => Write(ConsoleColor.Green, message, args);
	public static void WriteSuccessLine(string? message, params object?[] args) => WriteLine(ConsoleColor.Green, message, args);

	public static void Write(ConsoleColor foregroundColor, string? message, params object?[] args)
	{
		LastForegroundColor = foregroundColor;
		Console.ForegroundColor = foregroundColor;
		Console.Write(message ?? "", args);
		Console.ResetColor();
	}

	public static void WriteLine(ConsoleColor foregroundColor, string? message, params object?[] args)
	{
		Write(foregroundColor, message, args);
		Console.WriteLine();
	}
}
