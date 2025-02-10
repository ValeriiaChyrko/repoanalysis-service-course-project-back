using RepoAnalysis.DTOs;

namespace RepoAnalysis.Application.Abstractions;

public interface ICompilationService
{
    Task<int> VerifyProjectCompilation(RepositoryWithBranchQueryDto repoWithBranchQuery,
        CancellationToken cancellationToken = default);
}