using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.QualitySection;

namespace RepoAnalysis.Domain.Implementations.QualitySection;

public class PythonCodeAnalyzer : ICodeAnalyzer
{
    private const string DockerImage = "python:3.9-slim";
    private const string Command = "pylint";
    private readonly IDockerService _dockerService;
    private readonly ILogger<CodeQualityService> _logger;
    private readonly int _maxDegreeOfParallelism;

    public PythonCodeAnalyzer(IDockerService dockerService, ILogger<CodeQualityService> logger,
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

        var sourcePath = Path.Combine(repoPath, "src");
        var pythonFiles = Directory.GetFiles(sourcePath, "*.py", SearchOption.AllDirectories);
        if (pythonFiles.Length == 0)
        {
            _logger.LogWarning("No Python files found in source path: {SourcePath}", sourcePath);
            return Enumerable.Empty<DiagnosticMessage>();
        }

        var diagnosticsList = new ConcurrentBag<DiagnosticMessage>();

        await Parallel.ForEachAsync(pythonFiles,
            new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism },
            async (filePath, ct) => { await AnalyzeFileInDockerAsync(filePath, repoPath, diagnosticsList, ct); });

        return diagnosticsList.Distinct();
    }

    private void ValidateRepositoryPath(string repoPath)
    {
        if (!string.IsNullOrWhiteSpace(repoPath)) return;
        _logger.LogError("Repository path is null or empty.");
        throw new ArgumentException("Repository path cannot be null or empty.", nameof(repoPath));
    }

    private async Task AnalyzeFileInDockerAsync(string filePath, string repositoryPath,
        ConcurrentBag<DiagnosticMessage> diagnosticsList, CancellationToken cancellationToken)
    {
        var fileDirectory = Path.GetDirectoryName(filePath);
        if (fileDirectory == null) return;

        var arguments = $"--output-format=json {Path.GetFileName(filePath)}";
        var workingDirectory = Path.GetFileName(fileDirectory);

        try
        {
            var dockerOptions = new DockerCommandOptions(
                repositoryPath,
                workingDirectory,
                DockerImage,
                Command,
                arguments
            );
            var result = await _dockerService.RunCommandAsync(dockerOptions, cancellationToken);

            if (!string.IsNullOrEmpty(result.OutputDataReceived))
                ProcessPylintOutput(result.OutputDataReceived, filePath, diagnosticsList);
        }
        catch (Exception ex)
        {
            diagnosticsList.Add(new DiagnosticMessage
            {
                Message = $"Error analyzing file {filePath}: {ex.Message}",
                Severity = DiagnosticSeverity.Error.ToString()
            });
        }
    }

    private void ProcessPylintOutput(string output, string filePath, ConcurrentBag<DiagnosticMessage> diagnosticsList)
    {
        try
        {
            var pylintDiagnostics = JsonSerializer.Deserialize<List<PylintMessage>>(output);
            if (pylintDiagnostics == null) return;

            var filteredDiagnostics = pylintDiagnostics
                .Where(d => !string.IsNullOrEmpty(d.Message) && !string.IsNullOrEmpty(d.Type))
                .ToList();

            foreach (var diagnostic in filteredDiagnostics)
            {
                var severity = DetermineSeverity(diagnostic.Type);
                if (string.IsNullOrEmpty(severity)) continue;

                diagnosticsList.Add(new DiagnosticMessage
                {
                    Message = diagnostic.Message,
                    Severity = severity
                });
            }
        }
        catch (JsonException ex)
        {
            diagnosticsList.Add(new DiagnosticMessage
            {
                Message = $"Error deserializing pylint output for file {filePath}: {ex.Message}",
                Severity = DiagnosticSeverity.Error.ToString()
            });
        }
    }

    private static string? DetermineSeverity(string type)
    {
        return type.ToLower() switch
        {
            "error" => DiagnosticSeverity.Error.ToString(),
            "warning" => DiagnosticSeverity.Warning.ToString(),
            "info" => DiagnosticSeverity.Info.ToString(),
            _ => null
        };
    }
}