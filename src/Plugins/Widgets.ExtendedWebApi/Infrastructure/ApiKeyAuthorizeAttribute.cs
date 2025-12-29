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
    private const string VALID_API_KEY = "labaraque-api-key-2025";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Option 1: Check for API key in header
        if (context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            if (VALID_API_KEY.Equals(extractedApiKey))
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
