using OpenIddict.Validation.AspNetCore;
using RepoAnalysis.Grpc.Services;

namespace RepoAnalysis.Grpc;

public static class DependencyInjection
{
    public static void AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc();

        // Configure authentication
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        // Configure OpenIddict
        var issuer = configuration
            .GetRequiredSection("OpenIddictSettings")
            .GetValue<string>("Issuer");
        var clientId = configuration
                    .GetRequiredSection("OpenIddictSettings")
                    .GetValue<string>("ClientId");
        var clientSecret = configuration
                            .GetRequiredSection("OpenIddictSettings")
                            .GetValue<string>("ClientSecret");
        
        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(issuer!); 
                options.AddAudiences("repo_analysis_api"); 

                options.UseIntrospection()
                    .SetClientId(clientId!) 
                    .SetClientSecret(clientSecret!); 

                options.UseAspNetCore();
                options.UseSystemNetHttp();
            });

        // Configure authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy("repoAnalysisPolicy", policy =>
            {
                policy.RequireClaim("scope", "repo_analysis"); 
            });
    }
    
    public static void ConfigureEndpoints(this IApplicationBuilder app)
    {
        app.UseRouting();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<AccountsGrpcService>(); 
            endpoints.MapGrpcService<CompilationGrpcService>();
            endpoints.MapGrpcService<QualityGrpcService>();
            endpoints.MapGrpcService<TestsGrpcService>();
        });
    }
}