namespace RepoAnalysis.DTOs;

public interface IBaseQueryDto
{
    string RepoTitle { get; init; }
    string OwnerGitHubUsername { get; init; }
    string AuthorGitHubUsername { get; init; }
}