using RepoAnalysis.Domain.Abstractions.Contracts;

namespace RepoAnalysis.Domain.Abstractions.DockerRelated;

public interface IDockerService
{
    Task<ProcessResult> RunCommandAsync(DockerCommandOptions options, CancellationToken cancellationToken);
}