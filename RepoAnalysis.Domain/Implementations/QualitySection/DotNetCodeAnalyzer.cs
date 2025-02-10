using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.QualitySection;

namespace RepoAnalysis.Domain.Implementations.QualitySection;

public partial class DotNetCodeAnalyzer : ICodeAnalyzer
{
    private const string DockerImage = "mcr.microsoft.com/dotnet/sdk:7.0";
    private const string Command = "dotnet";
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeQualityService> _logger;
    private readonly int _maxDegreeOfParallelism;

    public DotNetCodeAnalyzer(IDockerService dockerService, ILogger<CodeQualityService> logger,
        int maxDegreeOfParallelism = 4)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public async Task<IEnumerable<DiagnosticMessage>> AnalyzeAsync(string repoPath,
        CancellationToken cancellationToken = default)
    {
        ValidateRepositoryPath(repoPath);

        var projectFiles = GetProjectFiles(repoPath);
        if (projectFiles.Length == 0)
        {
            _logger.LogWarning("No project files found in repository: {RepositoryPath}", repoPath);
            return Enumerable.Empty<DiagnosticMessage>();
        }

        var diagnosticsList = new ConcurrentBag<DiagnosticMessage>();

        await Parallel.ForEachAsync(projectFiles,
            new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
            async (projectFile, ct) => { await AnalyzeProjectAsync(projectFile, repoPath, diagnosticsList, ct); });

        return diagnosticsList.Distinct();
    }

    private void ValidateRepositoryPath(string repoPath)
    {
        if (!string.IsNullOrWhiteSpace(repoPath)) return;
        _logger.LogError("Repository path is null or empty.");
        throw new ArgumentException("Repository path cannot be null or empty.", nameof(repoPath));
    }

    private async Task AnalyzeProjectAsync(string projectFile, string repositoryPath,
        ConcurrentBag<DiagnosticMessage> diagnosticsList, CancellationToken cancellationToken)
    {
        try
        {
            var diagnostics = await AnalyzeProjectInDockerAsync(projectFile, repositoryPath, cancellationToken);
            foreach (var diagnostic in diagnostics) diagnosticsList.Add(diagnostic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project: {ProjectFile}", projectFile);
        }
    }

    private async Task<IEnumerable<DiagnosticMessage>> AnalyzeProjectInDockerAsync(string projectFile,
        string repositoryPath, CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(repositoryPath, projectFile);
        var arguments = $"build {Path.GetFileName(relativePath)} --no-incremental /nologo /v:q";
        var workingDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;

        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            workingDirectory,
            DockerImage,
            Command,
            arguments
        );
        var result = await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
        return ParseDiagnostics(result.OutputDataReceived);
    }

    private static string[] GetProjectFiles(string repositoryPath)
    {
        return Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories);
    }

    private static IEnumerable<DiagnosticMessage> ParseDiagnostics(string output)
    {
        var diagnostics = new ConcurrentBag<DiagnosticMessage>();
        var regex = MessageRegex();

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Parallel.ForEach(lines, line =>
        {
            var match = regex.Match(line);
            if (!match.Success) return;
            var severity = match.Groups["severity"].Value.Equals("error", StringComparison.OrdinalIgnoreCase)
                ? DiagnosticSeverity.Error
                : DiagnosticSeverity.Warning;

            var message =
                $"{match.Groups["code"].Value}: {match.Groups["message"].Value} [{match.Groups["project"].Value}]";

            diagnostics.Add(new DiagnosticMessage
            {
                Message = message,
                Severity = severity.ToString()
            });
        });

        return diagnostics;
    }

    [GeneratedRegex(@"(?<severity>error|warning) (?<code>CS\d*): (?<message>.+?) \[(?<project>.+?)\]",
        RegexOptions.IgnoreCase, "uk-UA")]
    private static partial Regex MessageRegex();
}