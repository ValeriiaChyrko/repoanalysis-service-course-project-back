using Microsoft.Extensions.DependencyInjection;
using RepoAnalysis.Application.Abstractions;
using RepoAnalysis.Application.Implementations;

namespace RepoAnalysis.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICompilationService, CompilationService>();
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<ITestsService, TestsService>();
    }
}