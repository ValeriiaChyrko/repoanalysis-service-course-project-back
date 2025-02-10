namespace RepoAnalysis.Domain.Abstractions.GitHubRelated;

public interface IGitHubBuildService
{
    Task<bool> CheckIfProjectCompilesAsync(string ownerGitHubUsername, string repoTitle, string branchTitle,
        string lastCommitSha, CancellationToken cancellationToken = default);

    Task<int> EvaluateProjectCodeQualityAsync(string ownerGitHubUsername, string repoTitle, string branchTitle,
        string lastCommitSha, CancellationToken cancellationToken = default);

    Task<int> EvaluateProjectCodePassedTestsAsync(string ownerGitHubUsername, string repoTitle, string branchTitle,
        string lastCommitSha, CancellationToken cancellationToken = default);
}