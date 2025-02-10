using Microsoft.Extensions.Logging;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Implementations;

public class CompilationService : ICompilationService
{
    private readonly ICommitService _commitService;
    private readonly IGitHubBuildService _gitHubBuildService;
    private readonly ILogger<CompilationService> _logger;

    public CompilationService(ICommitService commitService, IGitHubBuildService gitHubBuildService,
        ILogger<CompilationService> logger)
    {
        _commitService = commitService ?? throw new ArgumentNullException(nameof(commitService));
        _gitHubBuildService = gitHubBuildService ?? throw new ArgumentNullException(nameof(gitHubBuildService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> VerifyProjectCompilation(RepositoryWithBranchQueryDto repoWithBranchQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Verifying compilation for {Owner}/{Repo}, Branch: {Branch}, Author: {Author}",
            repoWithBranchQuery.OwnerGitHubUsername, repoWithBranchQuery.RepoTitle, repoWithBranchQuery.BranchTitle,
            repoWithBranchQuery.AuthorGitHubUsername);

        var lastCommitSha = await _commitService.GetLastCommitByAuthorAsync(repoWithBranchQuery, cancellationToken);

        if (lastCommitSha == null)
        {
            _logger.LogWarning("No commit found for {Author} in {Repo} ({Branch})",
                repoWithBranchQuery.AuthorGitHubUsername, repoWithBranchQuery.RepoTitle, repoWithBranchQuery.BranchTitle);
            throw new ArgumentNullException(nameof(lastCommitSha));
        }

        var percentage = await _gitHubBuildService.CheckIfProjectCompilesAsync(
            repoWithBranchQuery.OwnerGitHubUsername, repoWithBranchQuery.RepoTitle, repoWithBranchQuery.BranchTitle, lastCommitSha,
            cancellationToken)
            ? 100
            : 0;

        _logger.LogInformation("Compilation check completed. Score: {Score}%", percentage);

        return percentage;
    }
}