using Grpc.Core;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.DTOs;

namespace RepoAnalisys.Grpc.Services;

public class AccountsService : AccountsOperator.AccountsOperatorBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsService> _logger;

    public AccountsService(IAccountService accountService, ILogger<AccountsService> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<BranchesResponse> GetAuthorBranches(BranchQuery request, ServerCallContext context)
    {
        _logger.LogInformation(
            "Received GetAuthorBranches request for repo: {RepoTitle}, owner: {Owner}, author: {Author}, since: {Since}, until: {Until}",
            request.RepoTitle, request.OwnerGithubUsername, request.AuthorGithubUsername, request.Since, request.Until);

        try
        {
            var query = new BranchQueryDto
            {
                RepoTitle = request.RepoTitle,
                OwnerGitHubUsername = request.OwnerGithubUsername,
                AuthorGitHubUsername = request.AuthorGithubUsername,
                Since = request.Since.ToDateTime(),
                Until = request.Until.ToDateTime()
            };

            var branches = await _accountService.GetAuthorBranches(query, context.CancellationToken);

            if (branches == null) return new BranchesResponse();

            var branchTitles = branches as string[] ?? branches.ToArray();
            _logger.LogInformation("Found {BranchCount} branches for repo: {RepoTitle}", branchTitles.Length,
                request.RepoTitle);

            var response = new BranchesResponse();
            response.BranchTitles.AddRange(branchTitles);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAuthorBranches for repo: {RepoTitle}", request.RepoTitle);
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred while processing the request."));
        }
    }
}