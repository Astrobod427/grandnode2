using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.Attributes;

namespace Widgets.ExtendedWebApi.Controllers;

/// <summary>
/// Base controller for mobile API endpoints.
/// Accepts both Bearer (admin JWT) and FrontAuthentication (customer JWT) tokens.
/// </summary>
[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},FrontAuthentication")]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Route("api/mobile/[controller]")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v2")]
[Produces("application/json")]
public abstract class BaseMobileApiController : ControllerBase
{
}
