using RepoAnalysis.Domain.Abstractions.Contracts;

namespace RepoAnalysis.Domain.Abstractions.GitHubRelated;

public interface IGitHubClientProvider
{
    GitHubClientOptions GetGitHubClientOptions();
}