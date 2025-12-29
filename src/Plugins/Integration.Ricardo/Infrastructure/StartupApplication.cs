using Grand.Infrastructure;
using Integration.Ricardo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Ricardo.Infrastructure;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register HttpClient for RicardoApiClient
        services.AddHttpClient<RicardoApiClient>();

        // Register services
        services.AddScoped<RicardoApiClient>();
        services.AddScoped<RicardoProductService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        // No middleware configuration needed
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}
