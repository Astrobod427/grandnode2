namespace Widgets.ExtendedWebApi.DTOs;

/// <summary>
/// Shopping cart item DTO for mobile API
/// </summary>
public class ShoppingCartItemDto
{
    public string Id { get; set; }
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductSku { get; set; }
    public string ProductImageUrl { get; set; }
    public double UnitPrice { get; set; }
    public int Quantity { get; set; }
    public double SubTotal { get; set; }
    public string WarehouseId { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public bool IsFreeShipping { get; set; }
    public bool IsGiftVoucher { get; set; }
}

/// <summary>
/// Full shopping cart response DTO
/// </summary>
public class ShoppingCartDto
{
    public IList<ShoppingCartItemDto> Items { get; set; } = new List<ShoppingCartItemDto>();
    public int TotalItems { get; set; }
    public double SubTotal { get; set; }
    public string CurrencyCode { get; set; }
}

/// <summary>
/// Request DTO for adding item to cart
/// </summary>
public class AddToCartRequest
{
    public string ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string WarehouseId { get; set; }
    public IList<ProductAttributeDto> Attributes { get; set; }
}

/// <summary>
/// Request DTO for updating cart item quantity
/// </summary>
public class UpdateCartItemRequest
{
    public string ShoppingCartItemId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Product attribute for cart items
/// </summary>
public class ProductAttributeDto
{
    public string Key { get; set; }
    public string Value { get; set; }
}

/// <summary>
/// Response for cart operations with warnings
/// </summary>
public class CartOperationResult
{
    public bool Success { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();
    public ShoppingCartItemDto Item { get; set; }
}
