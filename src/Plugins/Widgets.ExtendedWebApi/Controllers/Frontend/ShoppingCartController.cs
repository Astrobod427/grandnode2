using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

/// <summary>
/// Shopping Cart API endpoints for mobile applications.
/// Accepts both Bearer (admin API) and FrontAuthentication (customer API) tokens.
/// Route: /api/mobile/ShoppingCart
/// </summary>
public class ShoppingCartController : BaseMobileApiController
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IProductService _productService;
    private readonly IPricingService _pricingService;
    private readonly IContextAccessor _contextAccessor;

    public ShoppingCartController(
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
    /// Get current customer's shopping cart
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;
        var currency = _contextAccessor.WorkContext.WorkingCurrency;

        var cartItems = await _shoppingCartService.GetShoppingCart(
            store.Id,
            ShoppingCartType.ShoppingCart);

        var items = new List<ShoppingCartItemDto>();
        double subTotal = 0;

        foreach (var item in cartItems)
        {
            var product = await _productService.GetProductById(item.ProductId);
            if (product == null) continue;

            var (unitPrice, _, _) = await _pricingService.GetUnitPrice(item, product);
            var itemSubTotal = unitPrice * item.Quantity;
            subTotal += itemSubTotal;

            items.Add(new ShoppingCartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku,
                ProductImageUrl = product.ProductPictures?.FirstOrDefault()?.PictureId,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                SubTotal = itemSubTotal,
                WarehouseId = item.WarehouseId,
                CreatedOnUtc = item.CreatedOnUtc,
                UpdatedOnUtc = item.UpdatedOnUtc,
                IsFreeShipping = item.IsFreeShipping,
                IsGiftVoucher = item.IsGiftVoucher
            });
        }

        var result = new ShoppingCartDto
        {
            Items = items,
            TotalItems = items.Sum(x => x.Quantity),
            SubTotal = subTotal,
            CurrencyCode = currency?.CurrencyCode ?? "USD"
        };

        return Ok(result);
    }

    /// <summary>
    /// Add a product to the shopping cart
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

        var (warnings, cartItem) = await _shoppingCartService.AddToCart(
            customer,
            request.ProductId,
            ShoppingCartType.ShoppingCart,
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

        // Get unit price for response
        var (unitPrice, _, _) = await _pricingService.GetUnitPrice(cartItem, product);

        return Ok(new CartOperationResult
        {
            Success = true,
            Warnings = warnings,
            Item = new ShoppingCartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku,
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity,
                SubTotal = unitPrice * cartItem.Quantity,
                CreatedOnUtc = cartItem.CreatedOnUtc
            }
        });
    }

    /// <summary>
    /// Update shopping cart item quantity
    /// </summary>
    [HttpPut("{itemId}")]
    public async Task<IActionResult> Update(string itemId, [FromBody] UpdateCartItemRequest request)
    {
        if (string.IsNullOrEmpty(itemId))
            return BadRequest(new { error = "Item ID is required" });

        if (request == null || request.Quantity < 1)
            return BadRequest(new { error = "Quantity must be at least 1" });

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        // Find the cart item
        var cartItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);
        var cartItem = cartItems.FirstOrDefault(x => x.Id == itemId);

        if (cartItem == null)
            return NotFound(new { error = "Cart item not found" });

        var warnings = await _shoppingCartService.UpdateShoppingCartItem(
            customer,
            itemId,
            cartItem.WarehouseId,
            cartItem.Attributes?.ToList(),
            cartItem.EnteredPrice,
            cartItem.RentalStartDateUtc,
            cartItem.RentalEndDateUtc,
            request.Quantity);

        if (warnings.Any())
        {
            return BadRequest(new CartOperationResult
            {
                Success = false,
                Warnings = warnings
            });
        }

        // Get updated item
        var product = await _productService.GetProductById(cartItem.ProductId);
        var updatedItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);
        var updatedItem = updatedItems.FirstOrDefault(x => x.Id == itemId);

        if (updatedItem != null && product != null)
        {
            var (unitPrice, _, _) = await _pricingService.GetUnitPrice(updatedItem, product);

            return Ok(new CartOperationResult
            {
                Success = true,
                Item = new ShoppingCartItemDto
                {
                    Id = updatedItem.Id,
                    ProductId = updatedItem.ProductId,
                    ProductName = product.Name,
                    ProductSku = product.Sku,
                    UnitPrice = unitPrice,
                    Quantity = updatedItem.Quantity,
                    SubTotal = unitPrice * updatedItem.Quantity,
                    UpdatedOnUtc = updatedItem.UpdatedOnUtc
                }
            });
        }

        return Ok(new CartOperationResult { Success = true });
    }

    /// <summary>
    /// Remove an item from the shopping cart
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

        // Find the cart item
        var cartItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);
        var cartItem = cartItems.FirstOrDefault(x => x.Id == itemId);

        if (cartItem == null)
            return NotFound(new { error = "Cart item not found" });

        await _shoppingCartService.DeleteShoppingCartItem(customer, cartItem);

        return Ok(new { success = true, message = "Item removed from cart" });
    }

    /// <summary>
    /// Clear all items from the shopping cart
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        var cartItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);

        foreach (var item in cartItems)
        {
            await _shoppingCartService.DeleteShoppingCartItem(customer, item);
        }

        return Ok(new { success = true, message = "Cart cleared" });
    }

    /// <summary>
    /// Get cart items count (quick summary)
    /// </summary>
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        var cartItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);

        return Ok(new
        {
            itemCount = cartItems.Count,
            totalQuantity = cartItems.Sum(x => x.Quantity)
        });
    }
}
