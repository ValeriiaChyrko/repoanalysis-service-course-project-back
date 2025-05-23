using RepoAnalysis.Application;
using RepoAnalysis.Domain;
using RepoAnalysis.DTOs;
using Serilog;

namespace RepoAnalysis.Grpc;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register services
        builder.Services.AddDtosServices();
        builder.Services.AddDomainServices();
        builder.Services.AddApplicationServices();
        builder.Services.AddGrpcServices(builder.Configuration);

        // Configure Serilog
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
        
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // Configure middleware and endpoints
        app.UseSerilogRequestLogging();
        app.ConfigureEndpoints();
        
        app.MapHealthChecks("/health");

        app.Run();
    }
}