using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Backend;

public class ShipmentController : BaseBackendApiController
{
    private readonly IShipmentService _shipmentService;
    private readonly IPermissionService _permissionService;
    private readonly IMediator _mediator;

    public ShipmentController(
        IShipmentService shipmentService,
        IPermissionService permissionService,
        IMediator mediator)
    {
        _shipmentService = shipmentService;
        _permissionService = permissionService;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var shipmentsPage = await _shipmentService.GetAllShipments();
        var dtos = shipmentsPage.Select(s => new ShipmentDto
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
        });

        return Ok(dtos);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var shipment = await _shipmentService.GetShipmentById(key);
        if (shipment == null)
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

    [HttpPost("{key}/SetAsShipped")]
    public async Task<IActionResult> SetAsShipped(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var shipment = await _shipmentService.GetShipmentById(key);
        if (shipment == null)
            return NotFound();

        await _mediator.Send(new ShipCommand { Shipment = shipment, NotifyCustomer = true });
        return Ok();
    }

    [HttpPost("{key}/SetAsDelivered")]
    public async Task<IActionResult> SetAsDelivered(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var shipment = await _shipmentService.GetShipmentById(key);
        if (shipment == null)
            return NotFound();

        await _mediator.Send(new DeliveryCommand { Shipment = shipment, NotifyCustomer = true });
        return Ok();
    }

    [HttpPost("{key}/SetTrackingNumber")]
    public async Task<IActionResult> SetTrackingNumber(string key, [FromBody] string trackingNumber)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var shipment = await _shipmentService.GetShipmentById(key);
        if (shipment == null)
            return NotFound();

        shipment.TrackingNumber = trackingNumber;
        await _shipmentService.UpdateShipment(shipment);
        return Ok();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var shipment = await _shipmentService.GetShipmentById(key);
        if (shipment == null)
            return NotFound();

        await _shipmentService.DeleteShipment(shipment);
        return NoContent();
    }
}
