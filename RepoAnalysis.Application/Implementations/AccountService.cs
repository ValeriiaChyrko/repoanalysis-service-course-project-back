using Microsoft.Extensions.Logging;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Implementations;

public class AccountService : IAccountService
{
    private readonly IBranchService _branchService;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IBranchService branchService, ILogger<AccountService> logger)
    {
        _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<string>?> GetAuthorBranches(
        BranchQueryDto query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching author branches for {Owner}/{Repo} by {Author} (Since: {Since}, Until: {Until})",
            query.OwnerGitHubUsername, query.RepoTitle, query.AuthorGitHubUsername, query.Since, query.Until);

        var branches = await _branchService.GetBranchesInfoAsync(query, cancellationToken);
        var branchTitles = branches
            .Select(b => b["name"]?.ToString())
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        if (!branchTitles.Any())
        {
            _logger.LogWarning("No branches found for {Repo} by {Owner}", query.RepoTitle, query.OwnerGitHubUsername);
            return Enumerable.Empty<string>();
        }

        _logger.LogInformation("Found {BranchCount} branches in {Repo} by {Owner}", branchTitles.Length,
            query.RepoTitle, query.OwnerGitHubUsername);

        var authorBranches = await _branchService.GetBranchesByAuthorAsync(query, branchTitles!, cancellationToken);

        _logger.LogInformation("Returning {BranchCount} branches for author {Author}", authorBranches?.Count,
            query.AuthorGitHubUsername);

        return authorBranches;
    }

    public async Task<string> PostAuthorBranch(BranchQueryDto query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching author branches for {Owner}/{Repo} by {Author} (Since: {Since}, Until: {Until})",
            query.OwnerGitHubUsername, query.RepoTitle, query.AuthorGitHubUsername, query.Since, query.Until);

        var createdBranchTitle = await _branchService.PostBranchByAuthorAsync(query, cancellationToken);

        if (string.IsNullOrWhiteSpace(createdBranchTitle))
        {
            _logger.LogWarning("Failed to create a new branch for repo: {RepoTitle}", query.RepoTitle);
            return string.Empty;
        }

        _logger.LogInformation("Returning branch title for author {Author}", query.AuthorGitHubUsername);

        return createdBranchTitle;
    }
}