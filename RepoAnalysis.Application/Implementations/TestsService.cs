using Microsoft.Extensions.Logging;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Implementations;

public class TestsService : ITestsService
{
    private readonly ICommitService _commitService;
    private readonly IGitHubBuildService _gitHubBuildService;
    private readonly ILogger<TestsService> _logger;

    public TestsService(
        ICommitService commitService,
        IGitHubBuildService gitHubBuildService,
        ILogger<TestsService> logger)
    {
        _commitService = commitService ?? throw new ArgumentNullException(nameof(commitService));
        _gitHubBuildService = gitHubBuildService ?? throw new ArgumentNullException(nameof(gitHubBuildService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> VerifyProjectTests(RepositoryWithBranchQueryDto repoWithBranchQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Verifying project tests for {Owner}/{Repo}, Branch: {Branch}, Author: {Author}",
            repoWithBranchQuery.OwnerGitHubUsername, repoWithBranchQuery.RepoTitle, repoWithBranchQuery.BranchTitle,
            repoWithBranchQuery.AuthorGitHubUsername);

        var lastCommitSha = await _commitService.GetLastCommitByAuthorAsync(repoWithBranchQuery, cancellationToken);

        if (lastCommitSha == null)
        {
            _logger.LogWarning("No commit found for {Author} in {Repo} ({Branch})",
                repoWithBranchQuery.AuthorGitHubUsername, repoWithBranchQuery.RepoTitle, repoWithBranchQuery.BranchTitle);
            throw new ArgumentNullException(nameof(lastCommitSha));
        }

        var percentage = await _gitHubBuildService.EvaluateProjectCodePassedTestsAsync(
            repoWithBranchQuery.OwnerGitHubUsername, repoWithBranchQuery.RepoTitle, repoWithBranchQuery.BranchTitle, lastCommitSha,
            cancellationToken);

        _logger.LogInformation("Project tests verification completed. Passed tests score: {Score}%", percentage);

        return percentage;
    }
}