using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using RepoAnalisys.Grpc;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Grpc.Services;

[Authorize(Policy = "repoAnalysisPolicy")]
public class QualityGrpcService : QualityOperator.QualityOperatorBase
{
    private readonly ILogger<QualityGrpcService> _logger;
    private readonly IQualityService _qualityService;

    public QualityGrpcService(IQualityService qualityService, ILogger<QualityGrpcService> logger)
    {
        _qualityService = qualityService ?? throw new ArgumentNullException(nameof(qualityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<QualityResponse> VerifyProjectQuality(RepositoryWithBranchQuery request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "Received VerifyProjectQuality request for repo: {RepoTitle}, branch: {BranchTitle}, owner: {Owner}, author: {Author}",
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

            var score = await _qualityService.VerifyProjectQuality(queryDto, context.CancellationToken);

            _logger.LogInformation(
                "Quality verification completed for repo: {RepoTitle}, branch: {BranchTitle}. Score: {Score}",
                request.RepoTitle, request.BranchTitle, score);

            return new QualityResponse { Score = score };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyProjectQuality for repo: {RepoTitle}, branch: {BranchTitle}",
                request.RepoTitle, request.BranchTitle);
            throw new RpcException(
                new Status(StatusCode.Internal, "An error occurred while verifying project quality."));
        }
    }
}