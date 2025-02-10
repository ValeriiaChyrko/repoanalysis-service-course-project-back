using System.Diagnostics;

namespace RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;

public interface IProcessService
{
    Task<ProcessResult> RunProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken);
}