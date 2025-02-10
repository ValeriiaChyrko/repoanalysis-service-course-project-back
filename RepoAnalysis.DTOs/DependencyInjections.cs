using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RepoAnalysis.DTOs.validators;

namespace RepoAnalysis.DTOs;

public static class DependencyInjections
{
    public static void AddDtosServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        services.AddScoped<IValidator<BranchQueryDto>, BranchQueryDtoValidator>();
        services.AddScoped<IValidator<CommitQueryDto>, CommitQueryDtoValidator>();
        services.AddScoped<IValidator<RepositoryWithBranchQueryDto>, RepositoryWithBranchDtoValidator>();
    }
}