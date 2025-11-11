using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Serilog;
using Serilog.Events;
using Zafiro.Mixins;

namespace Zafiro.Commands;

public class Command(Maybe<ILogger> logger, LogEventLevel logLevel = LogEventLevel.Debug) : ICommand
{
    public Maybe<ILogger> Logger { get; } = logger;
    private LogEventLevel LogLevel { get; } = logLevel;

    public async Task<Result<string>> Execute(string command,
        string arguments,
        string workingDirectory = "",
        Dictionary<string, string>? environmentVariables = null)
    {
        // Sanitize before logging
        LogCommandExecution(command, arguments, workingDirectory, environmentVariables);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                processStartInfo.Environment[key] = value;
        }

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var outputTask = ReadStreamAsync(process.StandardOutput);
        var errorTask = ReadStreamAsync(process.StandardError);

        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        var output = outputTask.Result;
        var error = errorTask.Result;
        var combinedOutput = BuildCombinedLogMessage(output, error);
        combinedOutput = SanitizeSensitiveInfo(combinedOutput);

        if (process.ExitCode == 0)
        {
            Logger.Write(LogLevel, "Command succeeded:\n{CombinedOutput}", combinedOutput);
            return Result.Success(output);
        }

        // On error, always log at Error level and include the full (sanitized) output to aid investigation
        Logger.Error("Command failed with exit code {ExitCode}:\n{CombinedOutput}",
            process.ExitCode,
            combinedOutput);
        return Result.Failure<string>($"Process failed with exit code {process.ExitCode}");
    }

    private void LogCommandExecution(string command,
        string arguments,
        string workingDirectory,
        Dictionary<string, string>? environmentVariables)
    {
        var safeArgs = SanitizeSensitiveInfo(arguments);

        Logger.Write(
            LogLevel,
            "Executing command: {Command} with arguments: {Arguments} in directory: {WorkingDirectory}",
            command,
            safeArgs,
            string.IsNullOrWhiteSpace(workingDirectory) ? "current" : workingDirectory
        );

        if (environmentVariables?.Count > 0)
        {
            var sanitizedEnv = environmentVariables.ToDictionary(
                kvp => kvp.Key,
                kvp => ContainsSensitiveKeywords(kvp.Key) || ContainsSensitiveKeywords(kvp.Value)
                    ? "***HIDDEN***"
                    : kvp.Value
            );

            Logger.Write(LogLevel, "Environment variables: {@EnvironmentVariables}", sanitizedEnv);
        }
    }

    private static bool ContainsSensitiveKeywords(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var keywords = new[] { "password", "pass", "pwd", "key", "secret", "token", "auth" };
        return keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeSensitiveInfo(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var patterns = new[]
        {
            // --password secret
            new Regex(@"(?<=--password\s+)\S+", RegexOptions.IgnoreCase),
            // -p secret
            new Regex(@"(?<=\b-p\s+)\S+", RegexOptions.IgnoreCase),
            // password=secret or token:secret
            new Regex(@"(?<=\b(password|pass|pwd|token|secret|key)\s*[:=]\s*)\S+", RegexOptions.IgnoreCase),
            // -p:PropertyName=value or /p:PropertyName=value where PropertyName contains sensitive keywords
            new Regex(@"(?<=(-p:|/p:)[^=]*\b(password|pass|pwd|token|secret|key|auth)\b[^=]*=)\S+", RegexOptions.IgnoreCase),
            // -p:PropertyContainingSensitiveWord=value (like AndroidSigningKeyPass, AndroidSigningStorePass, etc.)
            new Regex(@"(?<=(-p:|/p:)\w*(password|pass|pwd|token|secret|key|auth)\w*=)\S+", RegexOptions.IgnoreCase)
        };

        foreach (var rx in patterns)
            input = rx.Replace(input, "***HIDDEN***");

        return input;
    }

    private static async Task<string> ReadStreamAsync(StreamReader reader)
    {
        var builder = new StringBuilder();
        while (await reader.ReadLineAsync() is { } line)
            builder.AppendLine(line);
        return builder.ToString();
    }

    private static string BuildCombinedLogMessage(string output, string error)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(output))
        {
            builder.AppendLine("Standard Output:");
            builder.AppendLine(output);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            builder.AppendLine("Standard Error:");
            builder.AppendLine(error);
        }

        return builder.ToString().TrimEnd();
    }
}
