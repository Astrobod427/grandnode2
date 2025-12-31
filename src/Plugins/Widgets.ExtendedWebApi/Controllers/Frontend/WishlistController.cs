using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

/// <summary>
/// Wishlist API endpoints for mobile applications
/// Requires JWT authentication (FrontAuthentication)
/// </summary>
public class WishlistController : BaseFrontendApiController
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
    [HttpGet]
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
    [HttpPost]
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
    [HttpDelete("{itemId}")]
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
    [HttpPost("{itemId}/move-to-cart")]
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
    [HttpDelete]
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
    /// Get wishlist items count
    /// </summary>
    [HttpGet("count")]
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
