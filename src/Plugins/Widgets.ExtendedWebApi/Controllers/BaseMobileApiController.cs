using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.Attributes;

namespace Widgets.ExtendedWebApi.Controllers;

/// <summary>
/// Base controller for mobile API endpoints.
/// Uses Bearer (admin JWT) authentication - same token as /Api/Token/Create.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Route("api/mobile/[controller]")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v2")]
[Produces("application/json")]
public abstract class BaseMobileApiController : ControllerBase
{
}
