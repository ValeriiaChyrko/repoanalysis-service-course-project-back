namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class BuildResult
{
    public BuildResult(bool success, List<string> failedProjects)
    {
        Success = success;
        FailedProjects = failedProjects;
    }

    public bool Success { get; }
    public List<string> FailedProjects { get; }
}