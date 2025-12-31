using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace Widgets.ExtendedWebApi.Infrastructure;

/// <summary>
/// Configures OpenAPI documentation for Mobile API endpoints.
/// </summary>
public class MobileApiOpenApiStartup : IStartupApplication
{
    public const string MobileApiGroupName = "mobile";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var webHostEnvironment = services.BuildServiceProvider().GetService<IWebHostEnvironment>();

        // Only enable OpenAPI in development
        if (webHostEnvironment?.IsDevelopment() == true)
        {
            services.AddOpenApi(MobileApiGroupName, options =>
            {
                options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;

                // Document info
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "GrandNode Mobile API",
                        Version = "1.0",
                        Description = @"
REST API for mobile applications (React Native, Flutter, etc.).

## Authentication
All endpoints require a **Bearer JWT token** obtained from `/Api/Token/Create`.

**Important:** Password must be Base64 encoded!

## Quick Start
```bash
# 1. Encode password
echo -n 'YourPassword' | base64

# 2. Get token
curl -X POST 'https://your-domain.com/Api/Token/Create' \
  -H 'Content-Type: application/json' \
  -d '{""email"": ""user@example.com"", ""password"": ""BASE64_ENCODED_PASSWORD""}'

# 3. Use token
curl 'https://your-domain.com/api/mobile/ShoppingCart' \
  -H 'Authorization: Bearer YOUR_TOKEN'
```
",
                        Contact = new OpenApiContact
                        {
                            Name = "GrandNode Mobile API",
                            Email = "support@grandnode.com",
                            Url = new Uri("https://grandnode.com")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "GNU General Public License v3.0",
                            Url = new Uri("https://github.com/grandnode/grandnode2/blob/main/LICENSE")
                        }
                    };
                    return Task.CompletedTask;
                });

                // Bearer security scheme
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT token obtained from /Api/Token/Create. Password must be Base64 encoded."
                    };

                    // Apply security globally
                    document.SecurityRequirements.Add(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });

                    return Task.CompletedTask;
                });

                // Clear servers
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Servers.Clear();
                    return Task.CompletedTask;
                });
            });
        }
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment.IsDevelopment())
        {
            // Map Scalar UI for Mobile API documentation
            application.MapScalarApiReference("/scalar/" + MobileApiGroupName, options =>
            {
                options.WithTitle("GrandNode Mobile API");
                options.WithOpenApiRoutePattern("/openapi/" + MobileApiGroupName + ".json");
                options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }
    }

    public int Priority => 506; // After main OpenAPI startup (505)
    public bool BeforeConfigure => false;
}
