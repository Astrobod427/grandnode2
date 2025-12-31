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
/// Shopping Cart API endpoints for mobile applications.
/// </summary>
/// <remarks>
/// Provides full shopping cart management: view, add, update, remove items.
/// Requires Bearer JWT authentication from /Api/Token/Create.
/// </remarks>
[Tags("Shopping Cart")]
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
    /// <returns>Shopping cart with all items, prices, and totals</returns>
    /// <response code="200">Returns the shopping cart</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(ShoppingCartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// <param name="request">Product and quantity to add</param>
    /// <returns>Result with added item details</returns>
    /// <response code="200">Product added successfully</response>
    /// <response code="400">Invalid request or product warnings</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Product not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status400BadRequest)]
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
    /// <param name="itemId">Cart item ID</param>
    /// <param name="request">New quantity</param>
    /// <returns>Updated item details</returns>
    /// <response code="200">Item updated successfully</response>
    /// <response code="400">Invalid quantity or product warnings</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Cart item not found</response>
    [HttpPut("{itemId}")]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CartOperationResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// <param name="itemId">Cart item ID to remove</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Item removed successfully</response>
    /// <response code="400">Invalid item ID</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Cart item not found</response>
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
    /// <returns>Success confirmation</returns>
    /// <response code="200">Cart cleared successfully</response>
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

        var cartItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);

        foreach (var item in cartItems)
        {
            await _shoppingCartService.DeleteShoppingCartItem(customer, item);
        }

        return Ok(new { success = true, message = "Cart cleared" });
    }

    /// <summary>
    /// Get cart items count (quick summary for badge display)
    /// </summary>
    /// <returns>Item count and total quantity</returns>
    /// <response code="200">Returns cart count</response>
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

        var cartItems = await _shoppingCartService.GetShoppingCart(store.Id, ShoppingCartType.ShoppingCart);

        return Ok(new
        {
            itemCount = cartItems.Count,
            totalQuantity = cartItems.Sum(x => x.Quantity)
        });
    }
}
