namespace RepoAnalysis.Domain.Abstractions.Contracts;

public class GitHubClientOptions
{
    public GitHubClientOptions(string token)
    {
        Token = token;
    }

    public string Token { get; set; }
}