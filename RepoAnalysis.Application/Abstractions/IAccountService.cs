using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Abstractions;

public interface IAccountService
{
    Task<IEnumerable<string>?> GetAuthorBranches(BranchQueryDto query, CancellationToken cancellationToken = default);
}