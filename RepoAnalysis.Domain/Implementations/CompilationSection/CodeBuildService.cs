using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.CompilationSection;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;
using RepoAnalysis.Domain.Abstractions.DockerRelated;

namespace RepoAnalysis.Domain.Implementations.CompilationSection;

public class CodeBuildService : ICodeBuildService
{
    private readonly IDockerService _dockerService;
    private readonly ILanguageDetector _languageDetector;
    private readonly ILogger<CodeBuildService> _logger;

    public CodeBuildService(ILanguageDetector languageDetector, IDockerService dockerService,
        ILogger<CodeBuildService> logger)
    {
        _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> VerifyProjectCompilation(string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            _logger.LogError("Repository path is null or empty.");
            throw new ArgumentException("Repository path cannot be null or empty.", nameof(repositoryPath));
        }

        var language = _languageDetector.DetectMainLanguage(repositoryPath);
        ICodeBuilder builder = language switch
        {
            "C#" => new DotNetCodeBuilder(_dockerService, _logger),
            "Python" => new PythonCodeBuilder(_dockerService, _logger),
            "Java" => new JavaCodeBuilder(_dockerService, _logger),
            _ => throw new NotSupportedException($"Unsupported file type: {language}")
        };

        _logger.LogInformation("Starting compilation process for language: {Language}", language);
        var result = await builder.BuildProjectAsync(repositoryPath, cancellationToken);
        return result.Success;
    }
}