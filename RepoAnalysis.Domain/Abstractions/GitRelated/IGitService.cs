namespace RepoAnalysis.Domain.Abstractions.GitRelated;

public interface IGitService
{
    void CloneRepository(string ownerGitHubUsername, string repoTitle, string targetDirectory);
    void CheckoutBranch(string repoDirectory, string branchTitle);
    void CheckoutCommit(string repoDirectory, string commitSha);
}