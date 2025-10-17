using System;
using System.CommandLine;
using System.IO;
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

            var inputArg = new Argument<FileInfo>("input")
            {
                Description = "Input script file (.sh or .ps1)"
            };

            var toOption = new Option<string>("--to")
                {
                Description = "Target format (bash or pwsh)"
            };

            var outputOption = new Option<FileInfo?>("--output")
            {
                Description = "Output file (defaults to input with new extension)"
            };

            command.Add(inputArg);
            command.Add(toOption);
            command.Add(outputOption);

            command.SetAction(async (parseResult, cancellationToken) =>
            {
                var input = parseResult.GetValue(inputArg);
                var to = parseResult.GetValue(toOption);
                var output = parseResult.GetValue(outputOption);

                if (string.IsNullOrWhiteSpace(to) ||
                    (!string.Equals(to, "bash", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(to, "pwsh", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.Error.WriteLine("Target format must be 'bash' or 'pwsh'");
                    return 1;
                }

                await ConvertScriptAsync(input!, to.ToLowerInvariant(), output);
                return 0;
            });

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

            // Ensure output directory
            if (!string.IsNullOrEmpty(output!.DirectoryName))
                Directory.CreateDirectory(output.DirectoryName);

            // Write
            await File.WriteAllTextAsync(output.FullName, convertedContent);

            // Mark executable on Unix
            if (!OperatingSystem.IsWindows() && string.Equals(targetFormat, "bash", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.SetUnixFileMode(output.FullName,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }
                catch
                {
                    // best-effort; ignore
                }
            }

            Console.WriteLine($"Converted successfully: {output.FullName}");
        }

        private static string DetectFormat(string content)
        {
            if (content.Contains("#!/usr/bin/env bash", StringComparison.Ordinal) ||
                content.Contains("#!/bin/bash", StringComparison.Ordinal) ||
                content.Contains("export ", StringComparison.Ordinal))
                return "bash";

            if (content.Contains("#!/usr/bin/env pwsh", StringComparison.Ordinal) ||
                content.Contains("#!/usr/bin/pwsh", StringComparison.Ordinal) ||
                content.Contains("$env:", StringComparison.Ordinal) ||
                content.Contains("param(", StringComparison.Ordinal))
                return "pwsh";

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

                // Convert echo
                if (trimmed.StartsWith("echo ", StringComparison.Ordinal))
                {
                    var message = trimmed[5..].Trim();
                    pwsh.AppendLine($"Write-Host {QuotePwsh(message)}");
                    continue;
                }

                // Common command mappings
                var simple = trimmed
                    .Replace("pwd", "Get-Location", StringComparison.Ordinal)
                    .Replace("ls", "Get-ChildItem", StringComparison.Ordinal)
                    .Replace("cat ", "Get-Content ", StringComparison.Ordinal)
                    .Replace("mkdir -p", "New-Item -ItemType Directory -Force", StringComparison.Ordinal)
                    .Replace("rm -rf", "Remove-Item -Recurse -Force", StringComparison.Ordinal);

                if (!simple.Equals(trimmed, StringComparison.Ordinal))
                {
                    pwsh.AppendLine(simple);
                    continue;
                }

                // Simple env var replacements
                var converted = line
                    .Replace("$PATH", "$env:PATH", StringComparison.Ordinal)
                    .Replace("$HOME", "$HOME", StringComparison.Ordinal);

                if (!string.Equals(converted, line, StringComparison.Ordinal))
                {
                    pwsh.AppendLine(converted);
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
                if (trimmed.StartsWith("Write-Host ", StringComparison.Ordinal))
                {
                    var message = trimmed[11..].Trim();
                    bash.AppendLine($"echo {QuoteBash(message)}");
                    continue;
                }

                // Simple command maps
                var simple = trimmed
                    .Replace("Get-Location", "pwd", StringComparison.Ordinal)
                    .Replace("Get-ChildItem", "ls", StringComparison.Ordinal)
                    .Replace("Get-Content ", "cat ", StringComparison.Ordinal)
                    .Replace("New-Item -ItemType Directory -Force", "mkdir -p", StringComparison.Ordinal)
                    .Replace("Remove-Item -Recurse -Force", "rm -rf", StringComparison.Ordinal);

                if (!simple.Equals(trimmed, StringComparison.Ordinal))
                {
                    bash.AppendLine(simple);
                    continue;
                }

                // If simple variable replacements changed the line, use it
                if (!string.Equals(converted, line, StringComparison.Ordinal))
                {
                    bash.AppendLine(converted);
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

        private static string QuotePwsh(string message)
        {
            // Escape PowerShell backticks and double quotes
            return $"\"{message.Replace("`", "``").Replace("\"", "`\"")}\"";
        }

        private static string QuoteBash(string message)
        {
            // Escape bash double quotes with backslash
            return $"\"{message.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }
    }
}
