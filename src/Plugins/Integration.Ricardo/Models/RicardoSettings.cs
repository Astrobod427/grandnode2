using Grand.Domain.Configuration;

namespace Integration.Ricardo.Models;

public class RicardoSettings : ISettings
{
    /// <summary>
    /// Use sandbox environment for testing
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// ricardo.ch Partner ID (obtained from ricardo)
    /// </summary>
    public string PartnerId { get; set; }

    /// <summary>
    /// ricardo.ch Partner Key (obtained from ricardo)
    /// </summary>
    public string PartnerKey { get; set; }

    /// <summary>
    /// ricardo.ch Account Username
    /// </summary>
    public string AccountUsername { get; set; }

    /// <summary>
    /// ricardo.ch Account Password
    /// </summary>
    public string AccountPassword { get; set; }

    /// <summary>
    /// Enable automatic stock synchronization
    /// </summary>
    public bool EnableStockSync { get; set; } = false;

    /// <summary>
    /// Stock sync interval in minutes
    /// </summary>
    public int StockSyncIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Default article duration in days (1-10)
    /// </summary>
    public int DefaultArticleDurationDays { get; set; } = 7;

    /// <summary>
    /// Default ricardo category ID
    /// </summary>
    public int DefaultCategoryId { get; set; }

    /// <summary>
    /// Price markup percentage for ricardo listings (0-100)
    /// </summary>
    public decimal PriceMarkupPercentage { get; set; } = 0;

    /// <summary>
    /// Enable logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
