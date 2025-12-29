using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Widgets.ExtendedWebApi.Attributes;

namespace Widgets.ExtendedWebApi.Infrastructure;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register ModelValidationAttribute for ServiceFilter
        services.AddScoped<ModelValidationAttribute>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        // Authorization handled by AuthorizeApiAdmin filter
    }

    public int Priority => 499; // Right before endpoints (500)
    public bool BeforeConfigure => false; // After routing, before endpoints
}
