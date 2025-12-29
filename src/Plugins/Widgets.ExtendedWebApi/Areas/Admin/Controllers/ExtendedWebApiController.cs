using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Widgets.ExtendedWebApi.Areas.Admin.Controllers;

public class ExtendedWebApiController : BaseAdminPluginController
{
    public IActionResult Configure()
    {
        return View();
    }
}
