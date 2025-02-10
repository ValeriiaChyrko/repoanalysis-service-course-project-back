using System.Net.Http.Headers;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using RepoAnalysis.Domain.Abstractions.CompilationSection;
using RepoAnalysis.Domain.Abstractions.Contracts.Interfaces;
using RepoAnalysis.Domain.Abstractions.DockerRelated;
using RepoAnalysis.Domain.Abstractions.GitHubRelated;
using RepoAnalysis.Domain.Abstractions.GitRelated;
using RepoAnalysis.Domain.Abstractions.QualitySection;
using RepoAnalysis.Domain.Abstractions.TestsSection;
using RepoAnalysis.Domain.Implementations.CompilationSection;
using RepoAnalysis.Domain.Implementations.DockerRelated;
using RepoAnalysis.Domain.Implementations.GitHubRelated;
using RepoAnalysis.Domain.Implementations.GitRelated;
using RepoAnalysis.Domain.Implementations.Helpers;
using RepoAnalysis.Domain.Implementations.QualitySection;
using RepoAnalysis.Domain.Implementations.TestsSection;
using ProcessService = RepoAnalysis.Domain.Implementations.DockerRelated.ProcessService;

namespace RepoAnalysis.Domain;

public static class DependencyInjection
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IGitHubClientProvider, GitHubClientProvider>();
        services.AddSingleton<MSBuildWorkspace>(_ => MSBuildWorkspace.Create(new Dictionary<string, string>
        {
            { "AlwaysCompileMarkupFilesInSeparateDomain", "true" },
            { "Configuration", "Debug" },
            { "Platform", "AnyCPU" }
        }));

        services.AddHttpClient<IGitHubApiClient, GitHubApiApiClient>((provider, client) =>
        {
            var gitHubClientProvider = provider.GetService<IGitHubClientProvider>();
            var options = gitHubClientProvider!.GetGitHubClientOptions();

            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", options.Token);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RepoAnalysis API v1/1.0");
        });

        services.AddScoped<IGitService, GitService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<ICommitService, CommitService>();

        services.AddScoped<ICodeBuildService, CodeBuildService>();
        services.AddScoped<ICodeQualityService, CodeQualityService>();
        services.AddScoped<ICodeTestsService, CodeTestsService>();

        services.AddScoped<IGitHubBuildService, GitHubBuildService>();
        services.AddScoped<ILanguageDetector, LanguageDetector>();
        services.AddScoped<IProcessService, ProcessService>();
        services.AddScoped<IDockerService, DockerService>();

        services.AddScoped<ICodeAnalyzer, DotNetCodeAnalyzer>();
        services.AddScoped<ICodeAnalyzer, PythonCodeAnalyzer>();
        services.AddScoped<ICodeAnalyzer, JavaCodeAnalyzer>();

        services.AddScoped<ITestsRunner, DotNetTestsRunner>();
        services.AddScoped<ITestsRunner, PythonTestsRunner>();
        services.AddScoped<ITestsRunner, JavaTestsRunner>();
    }
}