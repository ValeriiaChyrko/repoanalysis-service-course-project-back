using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;
using RepoAnalysis.Domain.Abstractions.DockerRelated;

namespace RepoAnalysis.Domain.Implementations.DockerRelated;

public class DockerService : IDockerService
{
    private readonly ILogger<DockerService> _logger;
    private readonly IProcessService _processService;

    public DockerService(IProcessService processService, ILogger<DockerService> logger)
    {
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProcessResult> RunCommandAsync(DockerCommandOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Docker command execution...");

        var dockerCommand =
            $"docker run --rm -v \"{options.RepositoryPath}:/workspace\" -w /workspace/{options.WorkingDirectory} {options.DockerImage} {options.Command}{(string.IsNullOrWhiteSpace(options.Arguments) ? string.Empty : $" {options.Arguments}")}";

        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        var shellFileName = isWindows ? "cmd.exe" : "sh";
        var shellArguments = isWindows ? $"/c \"{dockerCommand.Replace("\\", "/")}\"" : $"-c \"{dockerCommand}\"";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = shellFileName,
            Arguments = shellArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var result = await _processService.RunProcessAsync(processStartInfo, cancellationToken);

        return result;
    }
}