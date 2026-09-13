using System.CommandLine;
using Moka.Docs.Serve;

namespace Moka.Docs.Cli.Commands;

/// <summary>
///     The hidden <c>repl-worker</c> command. <c>mokadocs serve</c> starts it to run REPL snippets in
///     a process it can kill when a snippet runs past its timeout.
/// </summary>
internal static class ReplWorkerCommand
{
	/// <summary>The command name <c>serve</c> starts.</summary>
	internal const string Name = "repl-worker";

	/// <summary>Creates the command.</summary>
	public static Command Create()
	{
		var parentOption = new Option<int?>("--parent-pid")
			{ Description = "Exit when the process with this id exits" };

		var command = new Command(Name, "Runs REPL snippets for mokadocs serve over standard input and output")
		{
			parentOption
		};
		command.Hidden = true;

		command.SetAction((parseResult, ct) => ReplWorker.RunAsync(
			Console.OpenStandardInput(),
			Console.OpenStandardOutput(),
			parseResult.GetValue(parentOption),
			ct));

		return command;
	}
}
