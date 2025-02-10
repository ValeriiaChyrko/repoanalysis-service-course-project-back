using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;

namespace RepoAnalysis.Domain.Implementations.DockerRelated;

public partial class ProcessService : IProcessService
{
    private static readonly Regex AnsiEscapeRegex = CompileAnsiEscapeRegex();
    private readonly ILogger<ProcessService> _logger;

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = startInfo;
        
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        _logger.LogInformation("Starting process: {Command} {Arguments}", startInfo.FileName, startInfo.Arguments);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            process.Start();
            
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            
            outputBuilder.Append(output);
            errorBuilder.Append(error);

            var cleanedOutput = RemoveAnsiEscapeCodes(outputBuilder.ToString());

            _logger.LogInformation("Process exited with code {ExitCode} in {ElapsedMilliseconds}ms",
                process.ExitCode, stopwatch.ElapsedMilliseconds);

            if (process.ExitCode != 0)
                _logger.LogWarning("Process finished with errors. ExitCode: {ExitCode}, Errors: {Errors}",
                    process.ExitCode, errorBuilder.ToString());

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                ErrorDataReceived = errorBuilder.ToString(),
                OutputDataReceived = cleanedOutput
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the process.");
            throw new InvalidOperationException("An error occurred while running the process.", ex);
        }
    }

    private static string RemoveAnsiEscapeCodes(string input)
    {
        return AnsiEscapeRegex.Replace(input, "");
    }

    [GeneratedRegex(@"\x1B\[[0-?9;]*[mK]")]
    private static partial Regex CompileAnsiEscapeRegex();
}