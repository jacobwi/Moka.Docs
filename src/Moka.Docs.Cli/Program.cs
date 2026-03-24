using System.CommandLine;
using Moka.Docs.Cli.Commands;

var rootCommand =
    new RootCommand(
        "MokaDocs — A modern, beautiful, extensible static documentation site generator for .NET projects.")
    {
        InitCommand.Create(),
        NewCommand.Create(),
        BuildCommand.Create(),
        ServeCommand.Create(),
        CleanCommand.Create(),
        InfoCommand.Create(),
        ValidateCommand.Create(),
        DoctorCommand.Create(),
        StatsCommand.Create()
    };

return await rootCommand.Parse(args).InvokeAsync();