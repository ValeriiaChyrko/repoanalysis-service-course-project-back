using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.Contracts;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;

namespace RepoAnalysis.Domain.Implementations.GitHubRelated;

public class GitHubClientProvider : IGitHubClientProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<GitHubClientProvider> _logger;

    public GitHubClientProvider(IConfiguration config, ILogger<GitHubClientProvider> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public GitHubClientOptions GetGitHubClientOptions()
    {
        var token = _config["GitHubClient:Token"];

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogCritical("GitHub API token is missing or empty in configuration.");
            throw new ArgumentNullException(nameof(token), "GitHub API token must be specified.");
        }

        _logger.LogInformation("GitHub API token successfully retrieved from configuration.");
        return new GitHubClientOptions(token);
    }
}