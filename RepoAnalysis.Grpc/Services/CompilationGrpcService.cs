using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using RepoAnalisys.Grpc;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Grpc.Services;

[Authorize]
public class CompilationGrpcService : CompilationOperator.CompilationOperatorBase
{
    private readonly ICompilationService _compilationService;
    private readonly ILogger<CompilationGrpcService> _logger;

    public CompilationGrpcService(ICompilationService compilationService, ILogger<CompilationGrpcService> logger)
    {
        _compilationService = compilationService ?? throw new ArgumentNullException(nameof(compilationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<CompilationResponse> VerifyProjectCompilation(RepositoryWithBranchQuery request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received VerifyProjectCompilation request for repo: {RepoTitle}, branch: {BranchTitle}, owner: {Owner}, author: {Author}",
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

            var score = await _compilationService.VerifyProjectCompilation(queryDto, context.CancellationToken);

            _logger.LogInformation(
                "Compilation verification completed for repo: {RepoTitle}, branch: {BranchTitle}. Score: {Score}",
                request.RepoTitle, request.BranchTitle, score);

            return new CompilationResponse { Score = score };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyProjectCompilation for repo: {RepoTitle}, branch: {BranchTitle}",
                request.RepoTitle, request.BranchTitle);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while verifying project compilation."));
        }
    }
}