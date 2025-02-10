using LibGit2Sharp;
using RepoAnalysis.Domain.Abstractions.GitRelated;

namespace RepoAnalysis.Domain.Implementations.GitRelated;

public class GitService : IGitService
{
    public void CloneRepository(string ownerGitHubUsername, string repoTitle, string targetDirectory)
    {
        if (Directory.Exists(targetDirectory)) Directory.Delete(targetDirectory, true);

        Repository.Clone($"https://github.com/{ownerGitHubUsername}/{repoTitle}.git", targetDirectory);
    }

    public void CheckoutBranch(string repoDirectory, string branchTitle)
    {
        using var repo = new Repository(repoDirectory);
        Commands.Checkout(repo, branchTitle);
    }

    public void CheckoutCommit(string repoDirectory, string commitSha)
    {
        using var repo = new Repository(repoDirectory);
        var commit = repo.Commits.FirstOrDefault(c => c.Sha.Equals(commitSha, StringComparison.OrdinalIgnoreCase));
        if (commit == null) throw new Exception("Commit not found.");
        Commands.Checkout(repo, commit);
    }
}