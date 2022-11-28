using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tenogy.Tools.FluentMigrator.Helpers;

public static class ConsoleLogger
{
	public static bool Available { get; set; }

	public static void LogDebug(string message, params object?[] args)
		=> Out(ConsoleColor.Gray, null, message, args);

	public static void LogInformation(string message, params object?[] args)
		=> Out(ConsoleColor.Blue, null, message, args);

	public static void LogWarning(string message, params object?[] args)
		=> Out(ConsoleColor.Yellow, null, message, args);

	public static void LogError(string message, params object?[] args)
		=> Out(ConsoleColor.Red, null, message, args);

	public static void LogWarning(Exception e, string message, params object?[] args)
		=> Out(ConsoleColor.Yellow, e, message, args);

	public static void LogError(Exception e, string message, params object?[] args)
		=> Out(ConsoleColor.Red, e, message, args);

	private static void Out(ConsoleColor color, Exception? e, string message, params object?[] args)
	{
		if (!Available) return;

		var type = color switch
		{
			ConsoleColor.Gray => "DBUG",
			ConsoleColor.Blue => "INFO",
			ConsoleColor.Yellow => "WARN",
			ConsoleColor.Red => "CRIT",
			_ => "???"
		};

		if (args.Any())
		{
			var i = 0;
			message = new Regex(@"\{(\w+)\}", RegexOptions.Compiled).Replace(message, _ => i < args.Length ? "{" + i++ + "}" : "?");
			message = string.Format(message, args);
		}

		ConsoleColored.Write(color, $"[{type} {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}]: ");
		Console.WriteLine(message);


		if (!string.IsNullOrEmpty(e?.StackTrace))
			Console.WriteLine(e.StackTrace);
	}
}
