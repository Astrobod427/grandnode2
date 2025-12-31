using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

/// <summary>
/// Checkout API endpoints for mobile applications
/// Requires JWT authentication (FrontAuthentication)
/// </summary>
public class CheckoutController : BaseFrontendApiController
{
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IOrderCalculationService _orderCalculationService;
    private readonly IProductService _productService;
    private readonly IPricingService _pricingService;
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;
    private readonly ICustomerService _customerService;
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    public CheckoutController(
        IShoppingCartService shoppingCartService,
        IOrderCalculationService orderCalculationService,
        IProductService productService,
        IPricingService pricingService,
        IPaymentService paymentService,
        IShippingService shippingService,
        ICustomerService customerService,
        IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _shoppingCartService = shoppingCartService;
        _orderCalculationService = orderCalculationService;
        _productService = productService;
        _pricingService = pricingService;
        _paymentService = paymentService;
        _shippingService = shippingService;
        _customerService = customerService;
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    /// Get checkout summary including cart, totals, addresses, and available options
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;
        var currency = _contextAccessor.WorkContext.WorkingCurrency;
        var warnings = new List<string>();

        // Get cart items
        var cartItems = await _shoppingCartService.GetShoppingCart(
            store.Id,
            ShoppingCartType.ShoppingCart);

        if (!cartItems.Any())
        {
            return BadRequest(new { error = "Shopping cart is empty" });
        }

        // Build cart DTO
        var cartDto = await BuildCartDto(cartItems, currency?.CurrencyCode ?? "USD");

        // Get order totals
        var (subTotalDiscount, _, subTotalWithoutDiscount, subTotalWithDiscount, _) =
            await _orderCalculationService.GetShoppingCartSubTotal(cartItems, true);

        var isFreeShipping = await _orderCalculationService.IsFreeShipping(cartItems);
        var (shippingTotal, _, _) = await _orderCalculationService.GetShoppingCartShippingTotal(cartItems);
        var (taxTotal, _) = await _orderCalculationService.GetTaxTotal(cartItems);
        var (orderTotal, _, _, _, _, _) = await _orderCalculationService.GetShoppingCartTotal(cartItems);

        var totals = new OrderTotalsDto
        {
            SubTotal = subTotalWithoutDiscount,
            SubTotalDiscount = subTotalDiscount,
            Shipping = shippingTotal,
            IsFreeShipping = isFreeShipping,
            Tax = taxTotal,
            Total = orderTotal,
            CurrencyCode = currency?.CurrencyCode ?? "USD"
        };

        // Get billing address
        BillingAddressDto billingAddress = null;
        if (customer.BillingAddress != null)
        {
            billingAddress = new BillingAddressDto
            {
                Id = customer.BillingAddress.Id,
                FirstName = customer.BillingAddress.FirstName,
                LastName = customer.BillingAddress.LastName,
                Email = customer.BillingAddress.Email,
                Company = customer.BillingAddress.Company,
                CountryId = customer.BillingAddress.CountryId,
                StateProvinceId = customer.BillingAddress.StateProvinceId,
                City = customer.BillingAddress.City,
                Address1 = customer.BillingAddress.Address1,
                Address2 = customer.BillingAddress.Address2,
                ZipPostalCode = customer.BillingAddress.ZipPostalCode,
                PhoneNumber = customer.BillingAddress.PhoneNumber
            };
        }
        else
        {
            warnings.Add("Billing address is not set");
        }

        // Get shipping address
        ShippingAddressDto shippingAddress = null;
        var requiresShipping = cartItems.RequiresShipping();

        if (requiresShipping)
        {
            if (customer.ShippingAddress != null)
            {
                shippingAddress = new ShippingAddressDto
                {
                    Id = customer.ShippingAddress.Id,
                    FirstName = customer.ShippingAddress.FirstName,
                    LastName = customer.ShippingAddress.LastName,
                    Email = customer.ShippingAddress.Email,
                    Company = customer.ShippingAddress.Company,
                    CountryId = customer.ShippingAddress.CountryId,
                    StateProvinceId = customer.ShippingAddress.StateProvinceId,
                    City = customer.ShippingAddress.City,
                    Address1 = customer.ShippingAddress.Address1,
                    Address2 = customer.ShippingAddress.Address2,
                    ZipPostalCode = customer.ShippingAddress.ZipPostalCode,
                    PhoneNumber = customer.ShippingAddress.PhoneNumber
                };
            }
            else
            {
                warnings.Add("Shipping address is not set");
            }
        }

        // Get available payment methods
        var paymentMethods = new List<PaymentMethodDto>();
        var availablePaymentMethods = await _paymentService.LoadActivePaymentMethods(
            customer, store.Id, filterByCountryId: customer.BillingAddress?.CountryId ?? "");

        foreach (var pm in availablePaymentMethods)
        {
            paymentMethods.Add(new PaymentMethodDto
            {
                SystemName = pm.SystemName,
                Name = pm.FriendlyName,
                Description = await pm.Description(),
                AdditionalFee = await _paymentService.GetAdditionalHandlingFee(cartItems, pm.SystemName)
            });
        }

        // Get available shipping options
        var shippingOptions = new List<ShippingOptionDto>();
        if (requiresShipping && customer.ShippingAddress != null)
        {
            var getShippingOptionResponse = await _shippingService.GetShippingOptions(
                customer,
                cartItems,
                customer.ShippingAddress,
                "",
                store);

            if (getShippingOptionResponse.Success)
            {
                foreach (var so in getShippingOptionResponse.ShippingOptions)
                {
                    shippingOptions.Add(new ShippingOptionDto
                    {
                        Name = so.Name,
                        Description = so.Description,
                        Rate = so.Rate,
                        ShippingRateProviderSystemName = so.ShippingRateProviderSystemName
                    });
                }
            }
            else
            {
                warnings.AddRange(getShippingOptionResponse.Errors);
            }
        }

        // Determine if order can be placed
        var canPlaceOrder = !warnings.Any() &&
                           billingAddress != null &&
                           (!requiresShipping || shippingAddress != null) &&
                           paymentMethods.Any() &&
                           (!requiresShipping || shippingOptions.Any());

        var result = new CheckoutSummaryDto
        {
            Cart = cartDto,
            Totals = totals,
            BillingAddress = billingAddress,
            ShippingAddress = shippingAddress,
            RequiresShipping = requiresShipping,
            AvailablePaymentMethods = paymentMethods,
            AvailableShippingOptions = shippingOptions,
            CanPlaceOrder = canPlaceOrder,
            Warnings = warnings
        };

        return Ok(result);
    }

    /// <summary>
    /// Place an order
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var store = _contextAccessor.StoreContext.CurrentStore;

        // Validate request
        if (string.IsNullOrEmpty(request?.PaymentMethodSystemName))
        {
            return BadRequest(new PlaceOrderResultDto
            {
                Success = false,
                Errors = new List<string> { "Payment method is required" }
            });
        }

        // Get cart
        var cartItems = await _shoppingCartService.GetShoppingCart(
            store.Id,
            ShoppingCartType.ShoppingCart);

        if (!cartItems.Any())
        {
            return BadRequest(new PlaceOrderResultDto
            {
                Success = false,
                Errors = new List<string> { "Shopping cart is empty" }
            });
        }

        // Check billing address
        if (customer.BillingAddress == null)
        {
            return BadRequest(new PlaceOrderResultDto
            {
                Success = false,
                Errors = new List<string> { "Billing address is required" }
            });
        }

        // Check shipping if required
        var requiresShipping = cartItems.RequiresShipping();
        if (requiresShipping)
        {
            if (customer.ShippingAddress == null)
            {
                return BadRequest(new PlaceOrderResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Shipping address is required" }
                });
            }

            // Set shipping option
            if (!string.IsNullOrEmpty(request.ShippingOptionName))
            {
                var shippingOption = new ShippingOption
                {
                    Name = request.ShippingOptionName,
                    ShippingRateProviderSystemName = request.ShippingRateProviderSystemName
                };
                await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.SelectedShippingOption, shippingOption, store.Id);
            }
        }

        // Set payment method
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.SelectedPaymentMethod, request.PaymentMethodSystemName, store.Id);

        // Set loyalty points usage
        if (request.UseLoyaltyPoints)
        {
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout, true, store.Id);
        }

        // Place order
        var placeOrderResult = await _mediator.Send(new PlaceOrderCommand());

        if (placeOrderResult.Success)
        {
            return Ok(new PlaceOrderResultDto
            {
                Success = true,
                OrderId = placeOrderResult.PlacedOrder.Id,
                OrderNumber = placeOrderResult.PlacedOrder.OrderNumber,
                OrderTotal = placeOrderResult.PlacedOrder.OrderTotal
            });
        }
        else
        {
            return BadRequest(new PlaceOrderResultDto
            {
                Success = false,
                Errors = placeOrderResult.Errors
            });
        }
    }

    /// <summary>
    /// Get customer addresses
    /// </summary>
    [HttpGet("addresses")]
    public IActionResult GetAddresses()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var addresses = customer.Addresses?.Select(a => new AddressDto
        {
            Id = a.Id,
            FirstName = a.FirstName,
            LastName = a.LastName,
            Email = a.Email,
            Company = a.Company,
            CountryId = a.CountryId,
            StateProvinceId = a.StateProvinceId,
            City = a.City,
            Address1 = a.Address1,
            Address2 = a.Address2,
            ZipPostalCode = a.ZipPostalCode,
            PhoneNumber = a.PhoneNumber
        }).ToList() ?? new List<AddressDto>();

        return Ok(new
        {
            addresses,
            billingAddressId = customer.BillingAddress?.Id,
            shippingAddressId = customer.ShippingAddress?.Id
        });
    }

    /// <summary>
    /// Set billing address
    /// </summary>
    [HttpPost("billing-address/{addressId}")]
    public async Task<IActionResult> SetBillingAddress(string addressId)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var address = customer.Addresses?.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            return NotFound(new { error = "Address not found" });

        customer.BillingAddress = address;
        await _customerService.UpdateBillingAddress(address, customer.Id);

        return Ok(new { success = true, message = "Billing address updated" });
    }

    /// <summary>
    /// Set shipping address
    /// </summary>
    [HttpPost("shipping-address/{addressId}")]
    public async Task<IActionResult> SetShippingAddress(string addressId)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var address = customer.Addresses?.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            return NotFound(new { error = "Address not found" });

        customer.ShippingAddress = address;
        await _customerService.UpdateShippingAddress(address, customer.Id);

        return Ok(new { success = true, message = "Shipping address updated" });
    }

    private async Task<ShoppingCartDto> BuildCartDto(IList<ShoppingCartItem> cartItems, string currencyCode)
    {
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

        return new ShoppingCartDto
        {
            Items = items,
            TotalItems = items.Sum(x => x.Quantity),
            SubTotal = subTotal,
            CurrencyCode = currencyCode
        };
    }
}
