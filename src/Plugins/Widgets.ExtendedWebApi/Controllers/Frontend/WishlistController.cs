using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

/// <summary>
/// Wishlist API endpoints for mobile applications.
/// </summary>
/// <remarks>
/// Manage customer wishlists: view, add, remove items, move to cart.
/// Requires Bearer JWT authentication from /Api/Token/Create.
/// </remarks>
[Tags("Wishlist")]
public class WishlistController : BaseMobileApiController
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IProductService _productService;
    private readonly IPricingService _pricingService;
    private readonly IContextAccessor _contextAccessor;

    public WishlistController(
        IShoppingCartService shoppingCartService,
        IProductService productService,
        IPricingService pricingService,
        IContextAccessor contextAccessor)
    {
        _shoppingCartService = shoppingCartService;
        _productService = productService;
        _pricingService = pricingService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Get current customer's wishlist
    /// </summary>
    /// <returns>Wishlist with all items</returns>
    /// <response code="200">Returns the wishlist</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;
        var currency = _contextAccessor.WorkContext.WorkingCurrency;

        var wishlistItems = await _shoppingCartService.GetShoppingCart(
            store.Id,
            ShoppingCartType.Wishlist);

        var items = new List<ShoppingCartItemDto>();

        foreach (var item in wishlistItems)
        {
            var product = await _productService.GetProductById(item.ProductId);
            if (product == null) continue;

            var (unitPrice, _, _) = await _pricingService.GetUnitPrice(item, product);

            items.Add(new ShoppingCartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku,
                ProductImageUrl = product.ProductPictures?.FirstOrDefault()?.PictureId,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                SubTotal = unitPrice * item.Quantity,
                CreatedOnUtc = item.CreatedOnUtc,
                UpdatedOnUtc = item.UpdatedOnUtc
            });
        }

        return Ok(new
        {
            items,
            totalItems = items.Count,
            currencyCode = currency?.CurrencyCode ?? "USD"
        });
    }

    /// <summary>
    /// Add a product to the wishlist
    /// </summary>
    /// <param name="request">Product to add</param>
    /// <returns>Result with added item details</returns>
    /// <response code="200">Product added successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Product not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.ProductId))
            return BadRequest(new { error = "ProductId is required" });

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        // Verify product exists
        var product = await _productService.GetProductById(request.ProductId);
        if (product == null)
            return NotFound(new { error = "Product not found" });

        // Convert attributes if provided
        var attributes = request.Attributes?
            .Select(a => new Grand.Domain.Common.CustomAttribute { Key = a.Key, Value = a.Value })
            .ToList();

        var (warnings, wishlistItem) = await _shoppingCartService.AddToCart(
            customer,
            request.ProductId,
            ShoppingCartType.Wishlist,
            store.Id,
            request.WarehouseId,
            attributes,
            null, // customerEnteredPrice
            null, // rentalStartDate
            null, // rentalEndDate
            request.Quantity > 0 ? request.Quantity : 1);

        if (warnings.Any())
        {
            return BadRequest(new CartOperationResult
            {
                Success = false,
                Warnings = warnings
            });
        }

        var (unitPrice, _, _) = await _pricingService.GetUnitPrice(wishlistItem, product);

        return Ok(new CartOperationResult
        {
            Success = true,
            Warnings = warnings,
            Item = new ShoppingCartItemDto
            {
                Id = wishlistItem.Id,
                ProductId = wishlistItem.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku,
                UnitPrice = unitPrice,
                Quantity = wishlistItem.Quantity,
                SubTotal = unitPrice * wishlistItem.Quantity,
                CreatedOnUtc = wishlistItem.CreatedOnUtc
            }
        });
    }

    /// <summary>
    /// Remove an item from the wishlist
    /// </summary>
    /// <param name="itemId">Wishlist item ID</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Item removed</response>
    /// <response code="400">Invalid item ID</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Item not found</response>
    [HttpDelete("{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return BadRequest(new { error = "Item ID is required" });

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        // Find the wishlist item
        var wishlistItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.Wishlist);
        var wishlistItem = wishlistItems.FirstOrDefault(x => x.Id == itemId);

        if (wishlistItem == null)
            return NotFound(new { error = "Wishlist item not found" });

        await _shoppingCartService.DeleteShoppingCartItem(customer, wishlistItem);

        return Ok(new { success = true, message = "Item removed from wishlist" });
    }

    /// <summary>
    /// Move an item from wishlist to shopping cart
    /// </summary>
    /// <param name="itemId">Wishlist item ID to move</param>
    /// <returns>Result with cart item details</returns>
    /// <response code="200">Item moved to cart</response>
    /// <response code="400">Invalid item ID or move failed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Wishlist item not found</response>
    [HttpPost("{itemId}/move-to-cart")]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToCart(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return BadRequest(new { error = "Item ID is required" });

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        // Find the wishlist item
        var wishlistItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.Wishlist);
        var wishlistItem = wishlistItems.FirstOrDefault(x => x.Id == itemId);

        if (wishlistItem == null)
            return NotFound(new { error = "Wishlist item not found" });

        // Add to cart
        var (warnings, cartItem) = await _shoppingCartService.AddToCart(
            customer,
            wishlistItem.ProductId,
            ShoppingCartType.ShoppingCart,
            store.Id,
            wishlistItem.WarehouseId,
            wishlistItem.Attributes?.ToList(),
            wishlistItem.EnteredPrice,
            wishlistItem.RentalStartDateUtc,
            wishlistItem.RentalEndDateUtc,
            wishlistItem.Quantity);

        if (warnings.Any())
        {
            return BadRequest(new CartOperationResult
            {
                Success = false,
                Warnings = warnings
            });
        }

        // Remove from wishlist
        await _shoppingCartService.DeleteShoppingCartItem(customer, wishlistItem);

        var product = await _productService.GetProductById(cartItem.ProductId);
        var (unitPrice, _, _) = await _pricingService.GetUnitPrice(cartItem, product);

        return Ok(new CartOperationResult
        {
            Success = true,
            Item = new ShoppingCartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = product?.Name,
                ProductSku = product?.Sku,
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity,
                SubTotal = unitPrice * cartItem.Quantity,
                CreatedOnUtc = cartItem.CreatedOnUtc
            }
        });
    }

    /// <summary>
    /// Clear all items from the wishlist
    /// </summary>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Wishlist cleared</response>
    /// <response code="401">Not authenticated</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Clear()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        var wishlistItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.Wishlist);

        foreach (var item in wishlistItems)
        {
            await _shoppingCartService.DeleteShoppingCartItem(customer, item);
        }

        return Ok(new { success = true, message = "Wishlist cleared" });
    }

    /// <summary>
    /// Get wishlist items count (for badge display)
    /// </summary>
    /// <returns>Number of items in wishlist</returns>
    /// <response code="200">Returns item count</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCount()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        var wishlistItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.Wishlist);

        return Ok(new
        {
            itemCount = wishlistItems.Count
        });
    }
}
