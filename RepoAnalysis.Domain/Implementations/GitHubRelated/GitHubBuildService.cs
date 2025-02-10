using Microsoft.Extensions.Logging;
using RepoAnalysis.Domain.Abstractions.CompilationSection;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.Domain.Abstractions.QualitySection;
using RepoAnalysis.Domain.Abstractions.TestsSection;

namespace RepoAnalysis.Domain.Implementations.GitHubRelated;

public class GitHubBuildService : IGitHubBuildService
{
    private readonly ICodeBuildService _codeBuildService;
    private readonly ICodeTestsService _codeTestsService;
    private readonly IGitService _gitService;
    private readonly ILogger<GitHubBuildService> _logger;
    private readonly ICodeQualityService _qualityService;

    public GitHubBuildService(IGitService gitService, ICodeBuildService codeBuildService,
        ICodeQualityService qualityService, ICodeTestsService codeTestsService, ILogger<GitHubBuildService> logger)
    {
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        _codeBuildService = codeBuildService ?? throw new ArgumentNullException(nameof(codeBuildService));
        _qualityService = qualityService ?? throw new ArgumentNullException(nameof(qualityService));
        _codeTestsService = codeTestsService ?? throw new ArgumentNullException(nameof(codeTestsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CheckIfProjectCompilesAsync(string ownerGitHubUsername, string repoTitle,
        string branchTitle, string lastCommitSha, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if project {RepoTitle} compiles for branch {BranchTitle}.", repoTitle,
            branchTitle);

        var repoDirectory = PrepareRepositoryAsync(ownerGitHubUsername, repoTitle, branchTitle, lastCommitSha);
        if (repoDirectory == null)
        {
            _logger.LogWarning("Skipping compilation check because repository preparation failed.");
            return false;
        }

        var result = await _codeBuildService.VerifyProjectCompilation(repoDirectory, cancellationToken);
        _logger.LogInformation(
            "Compilation check for project {RepoTitle} on branch {BranchTitle} resulted in {Result}.", repoTitle,
            branchTitle, result);

        return result;
    }

    public async Task<int> EvaluateProjectCodeQualityAsync(string ownerGitHubUsername, string repoTitle,
        string branchTitle, string lastCommitSha, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating code quality for project {RepoTitle} on branch {BranchTitle}.", repoTitle,
            branchTitle);

        var repoDirectory = PrepareRepositoryAsync(ownerGitHubUsername, repoTitle, branchTitle, lastCommitSha);
        if (repoDirectory == null)
        {
            _logger.LogWarning("Skipping code quality evaluation because repository preparation failed.");
            return 0;
        }

        var qualityScore = await _qualityService.CheckCodeQualityAsync(repoDirectory, cancellationToken);
        _logger.LogInformation("Code quality score for project {RepoTitle} on branch {BranchTitle} is {QualityScore}.",
            repoTitle, branchTitle, qualityScore);

        return qualityScore;
    }

    public async Task<int> EvaluateProjectCodePassedTestsAsync(string ownerGitHubUsername, string repoTitle,
        string branchTitle, string lastCommitSha, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running tests for project {RepoTitle} on branch {BranchTitle}.", repoTitle,
            branchTitle);

        var repoDirectory = PrepareRepositoryAsync(ownerGitHubUsername, repoTitle, branchTitle, lastCommitSha);
        if (repoDirectory == null)
        {
            _logger.LogWarning("Skipping tests evaluation because repository preparation failed.");
            return 0;
        }

        var passedTests = await _codeTestsService.CheckCodeTestsAsync(repoDirectory, cancellationToken);
        _logger.LogInformation("Project {RepoTitle} on branch {BranchTitle} passed {PassedTests} tests.", repoTitle,
            branchTitle, passedTests);

        return passedTests;
    }

    private string? PrepareRepositoryAsync(string ownerGitHubUsername, string repoTitle, string branchTitle,
        string lastCommitSha)
    {
        if (string.IsNullOrEmpty(lastCommitSha))
        {
            _logger.LogWarning("Last commit SHA is null or empty. Skipping repository preparation.");
            return null;
        }

        var repoDirectory = Path.Combine(Path.GetTempPath(), $"{repoTitle}-{branchTitle}");

        if (!Directory.Exists(repoDirectory))
        {
            _logger.LogInformation(
                "Cloning repository {RepoTitle} from user {OwnerGitHubUsername} into {RepoDirectory}.", repoTitle,
                ownerGitHubUsername, repoDirectory);
            _gitService.CloneRepository(ownerGitHubUsername, repoTitle, repoDirectory);
        }

        _logger.LogInformation("Checking out branch {BranchTitle} in repository {RepoDirectory}.", branchTitle,
            repoDirectory);
        _gitService.CheckoutBranch(repoDirectory, branchTitle);

        _logger.LogInformation("Checking out commit {CommitSha} in repository {RepoDirectory}.", lastCommitSha,
            repoDirectory);
        _gitService.CheckoutCommit(repoDirectory, lastCommitSha);

        return repoDirectory;
    }
}