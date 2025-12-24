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
        // Nothing to configure in pipeline
    }

    public int Priority => 510; // After Grand.Module.Api (505)
    public bool BeforeConfigure => false;
}
