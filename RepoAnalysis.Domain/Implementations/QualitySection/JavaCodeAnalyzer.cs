using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.QualitySection;

namespace RepoAnalysis.Domain.Implementations.QualitySection;

public class JavaCodeAnalyzer : ICodeAnalyzer
{
    private const string DockerImage = "maven:3.8.6-openjdk-11-slim";
    private const string Command = "mvn";
    private const string CompileArguments = "compile -e";
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeQualityService> _logger;
    private readonly int _maxDegreeOfParallelism;

    public JavaCodeAnalyzer(IDockerService dockerService, ILogger<CodeQualityService> logger,
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
            var diagnostics = await AnalyzeProjectInDockerAsync(repositoryPath, cancellationToken);
            foreach (var diagnostic in diagnostics) diagnosticsList.Add(diagnostic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project: {ProjectFile}", projectFile);
        }
    }

    private async Task<IEnumerable<DiagnosticMessage>> AnalyzeProjectInDockerAsync(string repositoryPath,
        CancellationToken cancellationToken)
    {
        var dockerOptions = new DockerCommandOptions(
            repositoryPath,
            string.Empty,
            DockerImage,
            Command,
            CompileArguments
        );

        var result = await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);
        return ParseDiagnostics(result.OutputDataReceived);
    }

    private static string[] GetProjectFiles(string repositoryPath)
    {
        return Directory.GetFiles(repositoryPath, "pom.xml", SearchOption.AllDirectories);
    }

    private static IEnumerable<DiagnosticMessage> ParseDiagnostics(string output)
    {
        if (string.IsNullOrWhiteSpace(output)) return Enumerable.Empty<DiagnosticMessage>();

        var diagnostics = new ConcurrentBag<DiagnosticMessage>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        Parallel.ForEach(lines, line =>
        {
            var severity = GetSeverityFromLine(line);
            if (severity == null) return;

            diagnostics.Add(new DiagnosticMessage
            {
                Message = line.Trim(),
                Severity = severity.Value.ToString()
            });
        });

        return diagnostics;
    }

    private static DiagnosticSeverity? GetSeverityFromLine(string line)
    {
        if (line.Contains("error", StringComparison.OrdinalIgnoreCase)) return DiagnosticSeverity.Error;

        if (line.Contains("warning", StringComparison.OrdinalIgnoreCase)) return DiagnosticSeverity.Warning;

        return null;
    }
}