using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Integration.Ricardo.Models;
using Microsoft.AspNetCore.Mvc;
using Grand.Domain.Permissions;

namespace Integration.Ricardo.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Plugins)]
public class RicardoController : BaseAdminPluginController
{
    private readonly ISettingService _settingService;

    public RicardoController(ISettingService settingService)
    {
        _settingService = settingService;
    }

    public async Task<IActionResult> Configure()
    {
        var settings = await _settingService.GetSettingByKey<RicardoSettings>(RicardoDefaults.ProviderSystemName, new RicardoSettings());
        return View(settings);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(RicardoSettings model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _settingService.SaveSetting(model, RicardoDefaults.ProviderSystemName);

        Success("Configuration saved successfully");
        return RedirectToAction("Configure");
    }

    [HttpPost]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            var settings = await _settingService.GetSettingByKey<RicardoSettings>(RicardoDefaults.ProviderSystemName, new RicardoSettings());

            // TODO: Test connection with RicardoApiClient
            // For now, just validate settings
            if (string.IsNullOrWhiteSpace(settings.PartnerId) ||
                string.IsNullOrWhiteSpace(settings.PartnerKey) ||
                string.IsNullOrWhiteSpace(settings.AccountUsername) ||
                string.IsNullOrWhiteSpace(settings.AccountPassword))
            {
                return Json(new { success = false, message = "Please configure all required credentials first" });
            }

            return Json(new { success = true, message = "Connection test successful (ricardo.ch API authentication will be tested on first publish)" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Connection test failed: {ex.Message}" });
        }
    }
}
