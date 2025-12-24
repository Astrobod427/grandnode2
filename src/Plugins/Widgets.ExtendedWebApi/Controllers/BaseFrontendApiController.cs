using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.Attributes;

namespace Widgets.ExtendedWebApi.Controllers;

[Authorize(AuthenticationSchemes = "FrontAuthentication")]
[ServiceFilter(typeof(ModelValidationAttribute))]
[Route("api/my/[controller]")]
[ApiExplorerSettings(IgnoreApi = false, GroupName = "v1")]
[Produces("application/json")]
public abstract class BaseFrontendApiController : ControllerBase
{
    public override ForbidResult Forbid()
    {
        return new ForbidResult("FrontAuthentication");
    }
}
