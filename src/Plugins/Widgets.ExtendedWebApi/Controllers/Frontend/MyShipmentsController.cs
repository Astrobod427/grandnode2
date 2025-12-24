using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

public class MyShipmentsController : BaseFrontendApiController
{
    private readonly IShipmentService _shipmentService;
    private readonly IOrderService _orderService;
    private readonly IContextAccessor _contextAccessor;

    public MyShipmentsController(
        IShipmentService shipmentService,
        IOrderService orderService,
        IContextAccessor contextAccessor)
    {
        _shipmentService = shipmentService;
        _orderService = orderService;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        // Get all customer orders
        var ordersPage = await _orderService.SearchOrders(customerId: customer.Id);
        var orderIds = ordersPage.Select(o => o.Id).ToList();

        // Get shipments for these orders
        var allShipments = new List<ShipmentDto>();
        foreach (var orderId in orderIds)
        {
            var shipments = await _shipmentService.GetShipmentsByOrder(orderId);
            allShipments.AddRange(shipments.Select(s => new ShipmentDto
            {
                Id = s.Id,
                ShipmentNumber = s.ShipmentNumber,
                OrderId = s.OrderId,
                TrackingNumber = s.TrackingNumber,
                TotalWeight = s.TotalWeight,
                ShippedDateUtc = s.ShippedDateUtc,
                DeliveryDateUtc = s.DeliveryDateUtc,
                AdminComment = s.AdminComment,
                CreatedOnUtc = s.CreatedOnUtc
            }));
        }

        return Ok(allShipments.AsQueryable());
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var shipment = await _shipmentService.GetShipmentById(key);
        if (shipment == null)
            return NotFound();

        // Verify the shipment belongs to the customer's order
        var order = await _orderService.GetOrderById(shipment.OrderId);
        if (order == null || order.CustomerId != customer.Id)
            return NotFound();

        var dto = new ShipmentDto
        {
            Id = shipment.Id,
            ShipmentNumber = shipment.ShipmentNumber,
            OrderId = shipment.OrderId,
            TrackingNumber = shipment.TrackingNumber,
            TotalWeight = shipment.TotalWeight,
            ShippedDateUtc = shipment.ShippedDateUtc,
            DeliveryDateUtc = shipment.DeliveryDateUtc,
            AdminComment = shipment.AdminComment,
            CreatedOnUtc = shipment.CreatedOnUtc
        };

        return Ok(dto);
    }
}
