using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Domain.Implementations.GitRelated;

public class BranchService : IBranchService
{
    private readonly IGitHubApiClient _gitHubApiClient;
    private readonly ILogger<BranchService> _logger;

    public BranchService(IGitHubApiClient gitHubApiClient, ILogger<BranchService> logger)
    {
        _gitHubApiClient = gitHubApiClient ?? throw new ArgumentNullException(nameof(gitHubApiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JArray> GetBranchesInfoAsync(BranchQueryDto branchQuery,
        CancellationToken cancellationToken = default)
    {
        if (branchQuery == null)
            throw new ArgumentNullException(nameof(branchQuery));

        var url = $"repos/{branchQuery.OwnerGitHubUsername}/{branchQuery.RepoTitle}/branches";
        _logger.LogInformation("Fetching branches info from URL: {Url}", url);

        try
        {
            return await _gitHubApiClient.GetJsonArrayAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching branches for repo '{Repo}'", branchQuery.RepoTitle);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<string>?> GetBranchesByAuthorAsync(
        BranchQueryDto branchQueryDto, IEnumerable<string> branchTitles, CancellationToken cancellationToken = default)
    {
        if (branchQueryDto == null)
            throw new ArgumentNullException(nameof(branchQueryDto));

        _logger.LogInformation("Searching branches by author '{Author}' in repository '{Repo}'",
            branchQueryDto.AuthorGitHubUsername, branchQueryDto.RepoTitle);

        var branchTasks =
            branchTitles.Select(branch => FetchCommitsForBranchAsync(branchQueryDto, branch, cancellationToken));
        var results = await Task.WhenAll(branchTasks);

        var studentBranches = results.Where(branch => branch != null).Select(branch => branch!).ToHashSet();

        if (studentBranches.Count == 0)
        {
            _logger.LogInformation("No branches found for author '{Author}' in repository '{Repo}'",
                branchQueryDto.AuthorGitHubUsername, branchQueryDto.RepoTitle);
            return new HashSet<string>();
        }

        _logger.LogInformation("Found {BranchCount} branches with commits from author '{Author}'",
            studentBranches.Count, branchQueryDto.AuthorGitHubUsername);
        return studentBranches;
    }

    private async Task<string?> FetchCommitsForBranchAsync(BranchQueryDto branchQueryDto, string branch,
        CancellationToken cancellationToken)
    {
        var url = BuildCommitsUrl(branchQueryDto.OwnerGitHubUsername, branchQueryDto.RepoTitle, branch,
            branchQueryDto.AuthorGitHubUsername, branchQueryDto.Since, branchQueryDto.Until);

        try
        {
            var commitList = await _gitHubApiClient.GetJsonArrayAsync(url, cancellationToken);
            return commitList.Count > 0 ? branch : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching commits for branch '{Branch}' in repo '{Repo}'", branch,
                branchQueryDto.RepoTitle);
            return null;
        }
    }

    private static string BuildCommitsUrl(string owner, string repo, string branch, string? author = null,
        DateTime? since = null, DateTime? until = null)
    {
        var url = $"repos/{owner}/{repo}/commits?sha={branch}";

        if (!string.IsNullOrEmpty(author)) url += $"&author={Uri.EscapeDataString(author)}";
        if (since.HasValue) url += $"&since={since.Value:o}";
        if (until.HasValue) url += $"&until={until.Value:o}";

        return url;
    }
}