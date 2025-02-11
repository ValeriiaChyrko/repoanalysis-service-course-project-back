using RepoAnalysis.Grpc.Services;

namespace RepoAnalysis.Grpc;

public static class DependencyInjection
{
    public static void AddGrpcServices(this IServiceCollection services)
    {
        services.AddGrpc();
    }
    
    public static void ConfigureEndpoints(this IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<AccountsGrpcService>(); 
            endpoints.MapGrpcService<CompilationGrpcService>();
            endpoints.MapGrpcService<QualityGrpcService>();
            endpoints.MapGrpcService<TestsGrpcService>();
        });
    }
}