using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

public class MyOrdersController : BaseFrontendApiController
{
    private readonly IOrderService _orderService;
    private readonly IContextAccessor _contextAccessor;

    public MyOrdersController(
        IOrderService orderService,
        IContextAccessor contextAccessor)
    {
        _orderService = orderService;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var ordersPage = await _orderService.SearchOrders(customerId: customer.Id);
        var dtos = ordersPage.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerEmail = o.CustomerEmail,
            OrderTotal = o.OrderTotal,
            OrderStatus = ((int)o.OrderStatusId).ToString(),
            PaymentStatus = o.PaymentStatusId.ToString(),
            ShippingStatus = o.ShippingStatusId.ToString(),
            CreatedOnUtc = o.CreatedOnUtc,
            PaidDateUtc = o.PaidDateUtc,
            CurrencyCode = o.CustomerCurrencyCode
        });

        return Ok(dtos);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var order = await _orderService.GetOrderById(key);
        if (order == null || order.CustomerId != customer.Id)
            return NotFound();

        var dto = new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            OrderTotal = order.OrderTotal,
            OrderStatus = ((int)order.OrderStatusId).ToString(),
            PaymentStatus = order.PaymentStatusId.ToString(),
            ShippingStatus = order.ShippingStatusId.ToString(),
            CreatedOnUtc = order.CreatedOnUtc,
            PaidDateUtc = order.PaidDateUtc,
            CurrencyCode = order.CustomerCurrencyCode
        };

        return Ok(dto);
    }
}
