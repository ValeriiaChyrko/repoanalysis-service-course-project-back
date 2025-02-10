using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;

namespace RepoAnalysis.Domain.Implementations.Helpers;

public class LanguageDetector : ILanguageDetector
{
    private static readonly Dictionary<string, string> LanguageExtensions = new()
    {
        { ".cs", "C#" },
        { ".csproj", "C#" },
        { ".py", "Python" },
        { ".java", "Java" }
    };

    private readonly ILogger<LanguageDetector> _logger;

    public LanguageDetector(ILogger<LanguageDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string DetectMainLanguage(string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            _logger.LogError("The directory '{RepositoryPath}' does not exist.", repositoryPath);
            throw new DirectoryNotFoundException($"The directory '{repositoryPath}' does not exist.");
        }

        _logger.LogInformation("Scanning repository: {RepositoryPath}", repositoryPath);

        var fileCounts = new ConcurrentDictionary<string, int>();

        try
        {
            var files = Directory.EnumerateFiles(repositoryPath, "*.*", SearchOption.AllDirectories);

            files.AsParallel().ForAll(file =>
            {
                var fileName = file.AsSpan();
                var lastDotIndex = fileName.LastIndexOf('.');
                var extension = lastDotIndex != -1 ? fileName[lastDotIndex..].ToString() : string.Empty;

                if (LanguageExtensions.TryGetValue(extension, out var language))
                    fileCounts.AddOrUpdate(language, 1, (_, count) => count + 1);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing files in repository '{RepositoryPath}'", repositoryPath);
            throw;
        }

        if (fileCounts.IsEmpty)
        {
            _logger.LogWarning("No recognized programming languages found in repository: {RepositoryPath}",
                repositoryPath);
            return "Unknown";
        }

        var mainLanguage = fileCounts.MaxBy(kvp => kvp.Value).Key;

        _logger.LogInformation("Detected main language: {MainLanguage} in repository: {RepositoryPath}", mainLanguage,
            repositoryPath);
        return mainLanguage;
    }
}