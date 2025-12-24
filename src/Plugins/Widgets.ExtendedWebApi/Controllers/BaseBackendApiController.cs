using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.Attributes;

namespace Widgets.ExtendedWebApi.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Route("api/admin/[controller]")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
[Produces("application/json")]
public abstract class BaseBackendApiController : ControllerBase
{
    public override ForbidResult Forbid()
    {
        return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
    }
}
