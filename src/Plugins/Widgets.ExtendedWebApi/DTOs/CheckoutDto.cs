namespace Widgets.ExtendedWebApi.DTOs;

/// <summary>
/// Checkout summary DTO for mobile API
/// </summary>
public class CheckoutSummaryDto
{
    public ShoppingCartDto Cart { get; set; }
    public OrderTotalsDto Totals { get; set; }
    public BillingAddressDto BillingAddress { get; set; }
    public ShippingAddressDto ShippingAddress { get; set; }
    public bool RequiresShipping { get; set; }
    public IList<PaymentMethodDto> AvailablePaymentMethods { get; set; } = new List<PaymentMethodDto>();
    public IList<ShippingOptionDto> AvailableShippingOptions { get; set; } = new List<ShippingOptionDto>();
    public bool CanPlaceOrder { get; set; }
    public IList<string> Warnings { get; set; } = new List<string>();
}

/// <summary>
/// Order totals for checkout
/// </summary>
public class OrderTotalsDto
{
    public double SubTotal { get; set; }
    public double SubTotalDiscount { get; set; }
    public double? Shipping { get; set; }
    public bool IsFreeShipping { get; set; }
    public double Tax { get; set; }
    public double? Total { get; set; }
    public string CurrencyCode { get; set; }
}

/// <summary>
/// Address DTO
/// </summary>
public class AddressDto
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Company { get; set; }
    public string CountryId { get; set; }
    public string CountryName { get; set; }
    public string StateProvinceId { get; set; }
    public string StateProvinceName { get; set; }
    public string City { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string ZipPostalCode { get; set; }
    public string PhoneNumber { get; set; }
}

/// <summary>
/// Billing address for checkout
/// </summary>
public class BillingAddressDto : AddressDto
{
}

/// <summary>
/// Shipping address for checkout
/// </summary>
public class ShippingAddressDto : AddressDto
{
}

/// <summary>
/// Payment method option
/// </summary>
public class PaymentMethodDto
{
    public string SystemName { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double AdditionalFee { get; set; }
    public string LogoUrl { get; set; }
}

/// <summary>
/// Shipping option
/// </summary>
public class ShippingOptionDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public double Rate { get; set; }
    public string ShippingRateProviderSystemName { get; set; }
}

/// <summary>
/// Request DTO for placing an order
/// </summary>
public class PlaceOrderRequest
{
    public string PaymentMethodSystemName { get; set; }
    public string ShippingOptionName { get; set; }
    public string ShippingRateProviderSystemName { get; set; }
    public string OrderComment { get; set; }
    public bool UseLoyaltyPoints { get; set; }
}

/// <summary>
/// Result of placing an order
/// </summary>
public class PlaceOrderResultDto
{
    public bool Success { get; set; }
    public string OrderId { get; set; }
    public int OrderNumber { get; set; }
    public double OrderTotal { get; set; }
    public IList<string> Errors { get; set; } = new List<string>();
}
