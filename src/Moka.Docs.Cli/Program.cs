using System.CommandLine;
using Moka.Docs.Cli.Commands;

var rootCommand =
	new RootCommand(
		"MokaDocs - A modern, beautiful, extensible static documentation site generator for .NET projects.")
	{
		InitCommand.Create(),
		NewCommand.Create(),
		BuildCommand.Create(),
		ServeCommand.Create(),
		CleanCommand.Create(),
		InfoCommand.Create(),
		ValidateCommand.Create(),
		DoctorCommand.Create(),
		StatsCommand.Create(),
		ReplWorkerCommand.Create()
	};

ParseResult parseResult = rootCommand.Parse(args);

// The REPL worker reports the exit code a snippet passes to Environment.Exit. System.CommandLine's
// process termination handling replaced it with 143 on net9.0, so the worker runs without it.
InvocationConfiguration? invocation = parseResult.CommandResult.Command.Name == ReplWorkerCommand.Name
	? new InvocationConfiguration { ProcessTerminationTimeout = null }
	: null;

return await parseResult.InvokeAsync(invocation);
