using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RepoAnalysis.Grpc.Services;

namespace RepoAnalysis.Grpc;

public static class DependencyInjection
{
    public static void AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc();

        // Configure JWT Bearer authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.Audience = configuration["Authorization:Audience"];
                options.MetadataAddress = configuration["Authorization:MetadataAddress"]!;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Authorization:ValidIssuer"],
                    ValidAudience = configuration["Authorization:Audience"],
                    RoleClaimType = "role"
                };
            });
        services.AddAuthorization();
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