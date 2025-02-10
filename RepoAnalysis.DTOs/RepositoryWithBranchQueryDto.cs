namespace RepoAnalysis.DTOs;

public class RepositoryWithBranchQueryDto : IBaseQueryDto
{
    public string RepoTitle { get; init; } = string.Empty;
    public string OwnerGitHubUsername { get; init; } = string.Empty;
    public string AuthorGitHubUsername { get; init; } = string.Empty;
    public string BranchTitle { get; init; } = string.Empty;
}