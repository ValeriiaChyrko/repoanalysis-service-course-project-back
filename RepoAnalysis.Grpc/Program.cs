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
        builder.Services.AddGrpcServices();
        builder.Services.AddDtosServices();
        builder.Services.AddApplicationServices();
        builder.Services.AddDomainServices();
        
        // Configure Serilog
        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

        var app = builder.Build();

        // Configure middleware and endpoints
        app.UseSerilogRequestLogging();
        app.ConfigureEndpoints(); 
        
        app.Run();
    }
}