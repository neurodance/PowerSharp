using System.CommandLine;
using System.Text;

namespace PowerSharp.CLI.Commands
{
    /// <summary>
    /// Convert bash scripts to PowerShell and vice versa.
    /// Solves the cross-platform scripting pain point.
    /// </summary>
    public class ConvertCommand
    {
        public static Command Create()
        {
            var command = new Command("convert",
                "Convert between bash and PowerShell scripts");

            var inputArg = new Argument<FileInfo>(
                "input",
                "Input script file (.sh or .ps1)");

            var toOption = new Option<string>(
                "--to",
                "Target format (bash or pwsh)")
            {
                IsRequired = true
            };
            toOption.AddValidator(result =>
            {
                var value = result.GetValueOrDefault<string>();
                if (value != "bash" && value != "pwsh")
                {
                    result.ErrorMessage = "Target format must be 'bash' or 'pwsh'";
                }
            });

            var outputOption = new Option<FileInfo?>(
                "--output",
                "Output file (defaults to input with new extension)");

            command.AddArgument(inputArg);
            command.AddOption(toOption);
            command.AddOption(outputOption);

            command.SetHandler(async (input, to, output) =>
            {
                await ConvertScriptAsync(input, to, output);
            }, inputArg, toOption, outputOption);

            return command;
        }

        private static async Task ConvertScriptAsync(
            FileInfo input,
            string targetFormat,
            FileInfo? output)
        {
            if (!input.Exists)
            {
                Console.Error.WriteLine($"Error: Input file not found: {input.FullName}");
                Environment.Exit(1);
                return;
            }

            // Determine output file
            output ??= new FileInfo(
                Path.ChangeExtension(
                    input.FullName,
                    targetFormat == "bash" ? ".sh" : ".ps1"));

            Console.WriteLine($"Converting {input.Name} to {targetFormat}...");

            // Read input
            var inputContent = await File.ReadAllTextAsync(input.FullName);

            // Detect source format
            var sourceFormat = input.Extension.ToLowerInvariant() switch
            {
                ".sh" => "bash",
                ".ps1" => "pwsh",
                _ => DetectFormat(inputContent)
            };

            if (sourceFormat == targetFormat)
            {
                Console.WriteLine("Warning: Source and target formats are the same.");
                return;
            }

            // Convert
            var convertedContent = sourceFormat == "bash"
                ? ConvertBashToPowerShell(inputContent)
                : ConvertPowerShellToBash(inputContent);

            // Write output
            await File.WriteAllTextAsync(output.FullName, convertedContent);

            Console.WriteLine($"Converted successfully: {output.FullName}");
        }

        private static string DetectFormat(string content)
        {
            // Simple heuristic detection
            if (content.Contains("$env:") || content.Contains("param("))
                return "pwsh";
            if (content.Contains("#!/bin/bash") || content.Contains("export "))
                return "bash";

            return "unknown";
        }

        private static string ConvertBashToPowerShell(string bashScript)
        {
            var pwsh = new StringBuilder();
            pwsh.AppendLine("# Converted from bash by PowerSharp");
            pwsh.AppendLine();

            var lines = bashScript.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip shebang
                if (trimmed.StartsWith("#!")) continue;

                // Convert comments
                if (trimmed.StartsWith("#"))
                {
                    pwsh.AppendLine(line);
                    continue;
                }

                // Convert export
                if (trimmed.StartsWith("export "))
                {
                    var varAssignment = trimmed.Substring(7);
                    var parts = varAssignment.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var varName = parts[0].Trim();
                        var varValue = parts[1].Trim().Trim('"', '\'');
                        pwsh.AppendLine($"$env:{varName} = \"{varValue}\"");
                        continue;
                    }
                }

                // Convert variable references
                var converted = line
                    .Replace("$PATH", "$env:PATH")
                    .Replace("$HOME", "$env:USERPROFILE");

                // Convert echo to Write-Host
                if (trimmed.StartsWith("echo "))
                {
                    var message = trimmed.Substring(5).Trim();
                    pwsh.AppendLine($"Write-Host {message}");
                    continue;
                }

                // Default: copy line as-is (with warning comment)
                if (!string.IsNullOrWhiteSpace(trimmed) &&
                    !trimmed.StartsWith("#"))
                {
                    pwsh.AppendLine($"# TODO: Manual conversion needed: {line}");
                }
                else
                {
                    pwsh.AppendLine(line);
                }
            }

            return pwsh.ToString();
        }

        private static string ConvertPowerShellToBash(string pwshScript)
        {
            var bash = new StringBuilder();
            bash.AppendLine("#!/bin/bash");
            bash.AppendLine("# Converted from PowerShell by PowerSharp");
            bash.AppendLine();

            var lines = pwshScript.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Convert comments
                if (trimmed.StartsWith("#"))
                {
                    bash.AppendLine(line);
                    continue;
                }

                // Convert environment variables
                if (trimmed.StartsWith("$env:"))
                {
                    var varAssignment = trimmed.Substring(5);
                    var parts = varAssignment.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var varName = parts[0].Trim();
                        var varValue = parts[1].Trim().Trim('"');
                        bash.AppendLine($"export {varName}=\"{varValue}\"");
                        continue;
                    }
                }

                // Convert variable references
                var converted = line
                    .Replace("$env:PATH", "$PATH")
                    .Replace("$env:USERPROFILE", "$HOME")
                    .Replace("$env:", "$");

                // Convert Write-Host to echo
                if (trimmed.StartsWith("Write-Host "))
                {
                    var message = trimmed.Substring(11).Trim();
                    bash.AppendLine($"echo {message}");
                    continue;
                }

                // Default: copy line as-is (with warning comment)
                if (!string.IsNullOrWhiteSpace(trimmed) &&
                    !trimmed.StartsWith("#"))
                {
                    bash.AppendLine($"# TODO: Manual conversion needed: {line}");
                }
                else
                {
                    bash.AppendLine(line);
                }
            }

            return bash.ToString();
        }
    }
}
