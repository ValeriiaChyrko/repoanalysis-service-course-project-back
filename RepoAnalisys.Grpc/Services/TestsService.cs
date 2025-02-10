using Grpc.Core;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.DTOs;

namespace RepoAnalisys.Grpc.Services;

public class TestsService : TestsOperator.TestsOperatorBase
{
    private readonly ILogger<TestsService> _logger;
    private readonly ITestsService _testsService;

    public TestsService(ITestsService testsService, ILogger<TestsService> logger)
    {
        _testsService = testsService ?? throw new ArgumentNullException(nameof(testsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<TestsResponse> VerifyProjectPassedTests(RepositoryWithBranchQuery request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received VerifyProjectPassedTests request for repo: {RepoTitle}, branch: {BranchTitle}, owner: {Owner}, author: {Author}",
            request.RepoTitle, request.BranchTitle, request.OwnerGithubUsername, request.AuthorGithubUsername);

        try
        {
            var queryDto = new RepositoryWithBranchQueryDto
            {
                RepoTitle = request.RepoTitle,
                BranchTitle = request.BranchTitle,
                OwnerGitHubUsername = request.OwnerGithubUsername,
                AuthorGitHubUsername = request.AuthorGithubUsername
            };

            var score = await _testsService.VerifyProjectTests(queryDto, context.CancellationToken);

            _logger.LogInformation(
                "Test verification completed for repo: {RepoTitle}, branch: {BranchTitle}. Score: {Score}",
                request.RepoTitle, request.BranchTitle, score);

            return new TestsResponse { Score = score };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyProjectPassedTests for repo: {RepoTitle}, branch: {BranchTitle}",
                request.RepoTitle, request.BranchTitle);
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred while verifying project tests."));
        }
    }
}