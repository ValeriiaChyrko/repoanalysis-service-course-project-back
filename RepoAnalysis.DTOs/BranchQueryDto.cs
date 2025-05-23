﻿namespace RepoAnalysis.DTOs;

public class BranchQueryDto : IBaseQueryDto
{
    public DateTime? Since { get; init; }
    public DateTime? Until { get; init; }
    public string RepoTitle { get; init; } = string.Empty;
    public string BaseBranch { get; init; } = "main";
    public string OwnerGitHubUsername { get; init; } = string.Empty;
    public string AuthorGitHubUsername { get; init; } = string.Empty;
}