using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Abstractions;

public interface ITestsService
{
    Task<int> VerifyProjectTests(RepositoryWithBranchQueryDto repoWithBranchQuery,
        CancellationToken cancellationToken = default);
}