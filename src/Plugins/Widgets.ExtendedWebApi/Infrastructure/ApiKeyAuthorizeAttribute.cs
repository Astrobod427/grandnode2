using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Widgets.ExtendedWebApi.Infrastructure;

/// <summary>
/// Authorization filter that validates API Key from X-API-Key header
/// </summary>
public class ApiKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private const string API_KEY_HEADER = "X-API-Key";
    private const string VALID_API_KEY = "labaraque-api-key-2025";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check for API key in header
        if (!context.HttpContext.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "API Key missing" });
            return;
        }

        if (!VALID_API_KEY.Equals(extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid API Key" });
            return;
        }

        // API key is valid - allow request to proceed
    }
}
