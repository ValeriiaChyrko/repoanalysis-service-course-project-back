namespace RepoAnalysis.DTOs;

public class BranchQueryDto : IBaseQueryDto
{
    public string RepoTitle { get; init; } = string.Empty;
    public string OwnerGitHubUsername { get; init; } = string.Empty;
    public string AuthorGitHubUsername { get; init; } = string.Empty;
    public DateTime? Since { get; init; }
    public DateTime? Until { get; init; }
}