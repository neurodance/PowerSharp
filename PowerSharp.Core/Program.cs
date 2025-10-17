using System.CommandLine;
using PowerSharp.CLI.Commands;

namespace PowerSharp.CLI;

/// <summary>
/// Entry point for the PowerSharp CLI tool.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PowerSharp - Cross-platform scripting utilities");

        // Add convert command
        rootCommand.Subcommands.Add(ConvertCommand.Create());

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }
}
