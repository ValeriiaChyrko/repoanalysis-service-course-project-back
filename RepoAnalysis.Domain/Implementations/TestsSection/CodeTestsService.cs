using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.TestsSection;

namespace RepoAnalysis.Domain.Implementations.TestsSection;

public class CodeTestsService : ICodeTestsService
{
    private const int MaxScorePercentage = 100;
    private const int MinScorePercentage = 0;
    private readonly IDockerService _dockerService;
    private readonly ILanguageDetector _languageDetector;
    private readonly ILogger<CodeTestsService> _logger;

    public CodeTestsService(ILanguageDetector languageDetector, IDockerService dockerService,
        ILogger<CodeTestsService> logger)
    {
        _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> CheckCodeTestsAsync(string repoDirectory, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repoDirectory))
        {
            _logger.LogError("Repository directory is null or empty.");
            throw new ArgumentException("Repository directory cannot be null or empty.", nameof(repoDirectory));
        }

        var language = _languageDetector.DetectMainLanguage(repoDirectory);
        _logger.LogInformation("Detected main language: {Language}", language);

        ITestsRunner runner = language switch
        {
            "C#" => new DotNetTestsRunner(_dockerService, _logger),
            "Python" => new PythonTestsRunner(_dockerService, _logger),
            "Java" => new JavaTestsRunner(_dockerService, _logger),
            _ => throw new NotSupportedException($"Unsupported file type: {language}")
        };

        var results = await runner.RunTestsAsync(repoDirectory, cancellationToken);
        var testResults = results.ToList();
        _logger.LogInformation("Test results obtained: {Count} results", testResults.Count);

        return EvaluateTestResults(testResults);
    }

    private static int EvaluateTestResults(List<TestResult>? testResults)
    {
        if (testResults == null || !testResults.Any()) return MaxScorePercentage;

        var results = testResults.ToList();
        var passed = results.Count(tr => tr.IsPassed);
        var failed = results.Count(tr => !tr.IsPassed);

        if (passed + failed == 0) return MaxScorePercentage;

        var score = (int)(passed / (double)(passed + failed) * MaxScorePercentage);
        return Math.Clamp(score, MinScorePercentage, MaxScorePercentage);
    }
}