using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.QualitySection;

namespace RepoAnalysis.Domain.Implementations.QualitySection;

public class CodeQualityService : ICodeQualityService
{
    private const int MaxScorePercentage = 100;
    private const int MinScorePercentage = 0;

    private static readonly Dictionary<DiagnosticSeverity, int> SeverityWeights = new()
    {
        { DiagnosticSeverity.Error, 35 },
        { DiagnosticSeverity.Warning, 20 },
        { DiagnosticSeverity.Info, 10 }
    };

    private readonly IDockerService _dockerService;
    private readonly ILanguageDetector _languageDetector;
    private readonly ILogger<CodeQualityService> _logger;

    public CodeQualityService(ILanguageDetector languageDetector, IDockerService dockerService,
        ILogger<CodeQualityService> logger)
    {
        _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> CheckCodeQualityAsync(string repoPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repoPath))
        {
            _logger.LogError("Repository path is null or empty.");
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repoPath));
        }

        var language = _languageDetector.DetectMainLanguage(repoPath);
        ICodeAnalyzer analyzer = language switch
        {
            "C#" => new DotNetCodeAnalyzer(_dockerService, _logger),
            "Python" => new PythonCodeAnalyzer(_dockerService, _logger),
            "Java" => new JavaCodeAnalyzer(_dockerService, _logger),
            _ => throw new NotSupportedException($"Unsupported file type: {language}")
        };

        var diagnostics = await analyzer.AnalyzeAsync(repoPath, cancellationToken);
        return EvaluateCodeQuality(diagnostics);
    }

    private static int EvaluateCodeQuality(IEnumerable<DiagnosticMessage>? diagnostics)
    {
        if (diagnostics == null) return MaxScorePercentage;

        var diagnosticMessages = diagnostics.ToList();
        if (diagnosticMessages.Count == 0) return MaxScorePercentage;

        var severityCounts = diagnosticMessages
            .GroupBy(d => d.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        var errorCount = severityCounts.GetValueOrDefault(DiagnosticSeverity.Error.ToString(), 0);
        var warningCount = severityCounts.GetValueOrDefault(DiagnosticSeverity.Warning.ToString(), 0);
        var infoCount = severityCounts.GetValueOrDefault(DiagnosticSeverity.Info.ToString(), 0);

        var score = MaxScorePercentage - (errorCount * SeverityWeights[DiagnosticSeverity.Error] +
                                          warningCount * SeverityWeights[DiagnosticSeverity.Warning] +
                                          infoCount * SeverityWeights[DiagnosticSeverity.Info]);

        return Math.Clamp(score, MinScorePercentage, MaxScorePercentage);
    }
}