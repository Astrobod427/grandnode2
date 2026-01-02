using Grand.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Widgets.ExtendedWebApi.Controllers.Mobile;

/// <summary>
/// Store settings API for mobile app - theme colors, store info
/// </summary>
[ApiController]
[Route("api/mobile/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public class StoreController : ControllerBase
{
    private readonly IStoreContext _storeContext;

    public StoreController(IStoreContext storeContext)
    {
        _storeContext = storeContext;
    }

    /// <summary>
    /// Get store settings including theme colors
    /// </summary>
    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
        var store = _storeContext.CurrentStore;

        // Get theme color from store settings or use default
        var primaryColor = "#2196F3"; // Default blue
        var secondaryColor = "#03DAC6";
        var storeName = store?.Name ?? "La Baraque Shop";

        return Ok(new
        {
            storeName,
            primaryColor,
            secondaryColor,
            currency = "CHF",
            language = "fr-FR"
        });
    }
}
