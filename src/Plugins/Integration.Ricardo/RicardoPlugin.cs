using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Integration.Ricardo;

/// <summary>
/// ricardo.ch Integration Plugin
/// </summary>
public class RicardoPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return RicardoDefaults.ConfigurationUrl;
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    public override async Task Install()
    {
        // Install default settings
        var settings = new Models.RicardoSettings
        {
            UseSandbox = true,
            EnableStockSync = false,
            StockSyncIntervalMinutes = 60,
            PriceMarkupPercentage = 0,
            DefaultCategoryId = 0,
            DefaultArticleDurationDays = 7
        };
        await settingService.SaveSetting(settings);

        // Install translations
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Integration.Ricardo.FriendlyName",
            "ricardo.ch Integration");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.UseSandbox",
            "Use Sandbox");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.UseSandbox.Hint",
            "Enable to use ricardo.ch sandbox environment for testing");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.PartnerId",
            "Partner ID");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.PartnerId.Hint",
            "Your ricardo.ch Partner ID");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.PartnerKey",
            "Partner Key");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.PartnerKey.Hint",
            "Your ricardo.ch Partner Key");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.AccountUsername",
            "Account Username");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.AccountUsername.Hint",
            "Your ricardo.ch account username");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.AccountPassword",
            "Account Password");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.AccountPassword.Hint",
            "Your ricardo.ch account password");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.PriceMarkupPercentage",
            "Price Markup (%)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.PriceMarkupPercentage.Hint",
            "Percentage to add to product prices when publishing to ricardo.ch");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.DefaultCategoryId",
            "Default Category ID");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.DefaultCategoryId.Hint",
            "Default ricardo.ch category ID for products");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.DefaultArticleDurationDays",
            "Article Duration (Days)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.DefaultArticleDurationDays.Hint",
            "How many days articles should be listed (1-10)");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.EnableStockSync",
            "Enable Stock Sync");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.EnableStockSync.Hint",
            "Automatically sync stock quantities to ricardo.ch");

        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.StockSyncIntervalMinutes",
            "Stock Sync Interval (Minutes)");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource(
            "Plugins.Integration.Ricardo.StockSyncIntervalMinutes.Hint",
            "How often to sync stock (15-1440 minutes)");

        await base.Install();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    public override async Task Uninstall()
    {
        // Delete settings
        await settingService.DeleteSetting<Models.RicardoSettings>();

        // Delete translations
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.UseSandbox");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.UseSandbox.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.PartnerId");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.PartnerId.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.PartnerKey");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.PartnerKey.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.AccountUsername");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.AccountUsername.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.AccountPassword");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.AccountPassword.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.PriceMarkupPercentage");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.PriceMarkupPercentage.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.DefaultCategoryId");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.DefaultCategoryId.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.DefaultArticleDurationDays");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.DefaultArticleDurationDays.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.EnableStockSync");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.EnableStockSync.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.StockSyncIntervalMinutes");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Integration.Ricardo.StockSyncIntervalMinutes.Hint");

        await base.Uninstall();
    }
}
