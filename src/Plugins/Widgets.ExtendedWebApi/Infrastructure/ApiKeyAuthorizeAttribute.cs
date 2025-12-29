using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Widgets.ExtendedWebApi.Infrastructure;

/// <summary>
/// Authorization filter that validates API Key OR admin permissions
/// </summary>
public class ApiKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string API_KEY_HEADER = "X-API-Key";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get API key from environment variable
        var validApiKey = Environment.GetEnvironmentVariable("GRANDNODE_API_KEY") ?? "default-api-key-change-me";

        // Option 1: Check for API key in header
        if (context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            if (validApiKey.Equals(extractedApiKey))
            {
                // API key is valid - allow request
                return;
            }

            // API key provided but invalid
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid API Key" });
            return;
        }

        // Option 2: Check if user has admin permissions (for UI usage)
        var permissionService = context.HttpContext.RequestServices.GetService(typeof(IPermissionService)) as IPermissionService;
        if (permissionService != null)
        {
            var hasPermission = await permissionService.Authorize(StandardPermission.ManageProducts);
            if (hasPermission)
            {
                // User has admin permissions - allow request
                return;
            }
        }

        // Neither API key nor admin permissions
        context.Result = new UnauthorizedObjectResult(new { error = "Unauthorized - API Key or admin permissions required" });
    }
}
