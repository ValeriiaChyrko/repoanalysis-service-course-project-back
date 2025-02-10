using RepoAnalysis.Application;
using RepoAnalysis.Application.Implementations;
using RepoAnalysis.Domain;
using RepoAnalysis.DTOs;
using Serilog;

namespace RepoAnalisys.Grpc;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddGrpc();
        
        builder.Services.AddDtosServices();
        builder.Services.AddApplicationServices();
        builder.Services.AddDomainServices();
        
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

        var app = builder.Build();

        app.UseSerilogRequestLogging();
        
        app.MapGrpcService<AccountService>();
        app.MapGrpcService<CompilationService>();
        app.MapGrpcService<QualityService>();
        app.MapGrpcService<TestsService>();
        
        app.Run();
    }
}