using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Domain.Implementations.GitRelated;

public class CommitService : ICommitService
{
    private readonly IGitHubApiClient _gitHubApiClient;
    private readonly ILogger<CommitService> _logger;

    public CommitService(IGitHubApiClient gitHubApiClient, ILogger<CommitService> logger)
    {
        _gitHubApiClient = gitHubApiClient ?? throw new ArgumentNullException(nameof(gitHubApiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JArray> GetCommitsForBranchAsync(RepositoryWithBranchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var url = $"repos/{query.OwnerGitHubUsername}/{query.RepoTitle}/commits?sha={query.BranchTitle}";
        _logger.LogInformation("Fetching commits from {Repo} on branch {Branch} by {Owner}",
            query.RepoTitle, query.BranchTitle, query.OwnerGitHubUsername);

        var commitListJson = await _gitHubApiClient.GetJsonArrayAsync(url, cancellationToken);
        _logger.LogInformation("Fetched {Count} commits", commitListJson.Count);

        return commitListJson;
    }

    public async Task<IEnumerable<string?>> FilterCommitsByAuthorAsync(CommitQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var url = BuildCommitsUrl(
            query.OwnerGitHubUsername,
            query.RepoTitle,
            query.BranchTitle,
            query.AuthorGitHubUsername,
            query.Since,
            query.Until
        );
        _logger.LogInformation("Fetching commits for {Repo} on branch {Branch} by author {Author}",
            query.RepoTitle, query.BranchTitle, query.AuthorGitHubUsername);

        var commitListJson = await _gitHubApiClient.GetJsonArrayAsync(url, cancellationToken);
        var commitShas = commitListJson.Select(commit => commit["sha"]?.ToString()).ToList();

        _logger.LogInformation("Found {Count} commits for author {Author}", commitShas.Count,
            query.AuthorGitHubUsername);
        return commitShas;
    }

    public async Task<string?> GetLastCommitByAuthorAsync(RepositoryWithBranchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var url = BuildCommitsUrl(
            query.OwnerGitHubUsername,
            query.RepoTitle,
            query.BranchTitle,
            query.AuthorGitHubUsername
        ) + "&per_page=1";
        _logger.LogInformation("Fetching last commit for {Repo} on branch {Branch} by author {Author}",
            query.RepoTitle, query.BranchTitle, query.AuthorGitHubUsername);

        var commitListJson = await _gitHubApiClient.GetJsonArrayAsync(url, cancellationToken);
        var lastCommitSha = commitListJson.Count > 0 ? commitListJson[0]["sha"]?.ToString() : null;

        _logger.LogInformation("Last commit SHA: {Sha}", lastCommitSha ?? "None");
        return lastCommitSha;
    }

    private static string BuildCommitsUrl(string ownerGitHubUsername, string repoTitle, string branchTitle,
        string authorGitHubUsername, DateTime? since = null, DateTime? until = null)
    {
        var url = $"repos/{ownerGitHubUsername}/{repoTitle}/commits?sha={branchTitle}&author={authorGitHubUsername}";

        if (since.HasValue) url += $"&since={since.Value:o}";
        if (until.HasValue) url += $"&until={until.Value:o}";

        return url;
    }
}