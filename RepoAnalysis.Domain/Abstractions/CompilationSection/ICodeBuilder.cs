using RepoAnalysis.Domain.Abstractions.Contracts;

namespace RepoAnalysis.Domain.Abstractions.CompilationSection;

public interface ICodeBuilder
{
    Task<BuildResult> BuildProjectAsync(string repositoryPath, CancellationToken cancellationToken);
}