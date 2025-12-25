using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Widgets.ExtendedWebApi.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Widgets)]
public class ExtendedWebApiController : BaseAdminPluginController
{
    public IActionResult Configure()
    {
        return View("~/Plugins/Widgets.ExtendedWebApi/Views/Configure.cshtml");
    }
}
