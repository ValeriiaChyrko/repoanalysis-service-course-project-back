using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using RepoAnalisys.Grpc;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.DTOs;

namespace RepoAnalysis.Grpc.Services;

[Authorize]
public class AccountsGrpcService : AccountsOperator.AccountsOperatorBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsGrpcService> _logger;

    public AccountsGrpcService(IAccountService accountService, ILogger<AccountsGrpcService> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<BranchesResponse> GetAuthorBranches(BranchQuery request, ServerCallContext context)
    {
        _logger.LogInformation(
            "Received GetAuthorBranches request for repo: {RepoTitle}, owner: {Owner}, author: {Author}, since: {Since}, until: {Until}",
            request.RepoTitle, request.OwnerGithubUsername, request.AuthorGithubUsername,
            request.Since?.ToDateTime(), request.Until?.ToDateTime());

        try
        {
            var query = new BranchQueryDto
            {
                RepoTitle = request.RepoTitle,
                OwnerGitHubUsername = request.OwnerGithubUsername,
                AuthorGitHubUsername = request.AuthorGithubUsername,
                Since = request.Since?.ToDateTime(),
                Until = request.Until?.ToDateTime()
            };

            var branches = await _accountService.GetAuthorBranches(query, context.CancellationToken);
            var branchTitles = branches?.ToArray();

            if (branchTitles == null || branchTitles.Length == 0)
            {
                _logger.LogInformation("No branches found for repo: {RepoTitle}", request.RepoTitle);
                return new BranchesResponse();
            }

            var response = new BranchesResponse();
            response.BranchTitles.AddRange(branchTitles);

            _logger.LogInformation("Found {BranchCount} branches for repo: {RepoTitle}", branchTitles.Length,
                request.RepoTitle);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAuthorBranches for repo: {RepoTitle}", request.RepoTitle);
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred while processing the request."));
        }
    }
}