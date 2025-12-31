using Grand.Infrastructure;
using Grand.Module.Api.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grand.Module.Api.Infrastructure;

public class CorsStartup : IStartupApplication
{
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        // Use permissive CORS for all environments - JWT auth is the real security layer
        application.UseCors(Configurations.DevelopmentCorsPolicyName);
    }

    public void ConfigureServices(IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            // Allow any origin - suitable for multi-tenant e-commerce with JWT authentication
            options.AddPolicy(Configurations.DevelopmentCorsPolicyName,
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy(Configurations.ProductionCorsPolicyName,
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
    }

    public int Priority => 0;
    public bool BeforeConfigure => true;
}