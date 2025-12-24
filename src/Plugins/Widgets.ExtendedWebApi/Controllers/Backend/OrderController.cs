using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Backend;

public class OrderController : BaseBackendApiController
{
    private readonly IOrderService _orderService;
    private readonly IPermissionService _permissionService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly IMediator _mediator;

    public OrderController(
        IOrderService orderService,
        IPermissionService permissionService,
        IPaymentTransactionService paymentTransactionService,
        IMediator mediator)
    {
        _orderService = orderService;
        _permissionService = permissionService;
        _paymentTransactionService = paymentTransactionService;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var ordersPage = await _orderService.SearchOrders();
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
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var order = await _orderService.GetOrderById(key);
        if (order == null)
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

    [HttpPost("{key}/MarkAsPaid")]
    public async Task<IActionResult> MarkAsPaid(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var order = await _orderService.GetOrderById(key);
        if (order == null)
            return NotFound();

        var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (paymentTransaction == null)
            return BadRequest("No payment transaction found for this order");

        await _mediator.Send(new MarkAsPaidCommand { PaymentTransaction = paymentTransaction });
        return Ok();
    }

    [HttpPost("{key}/MarkAsAuthorized")]
    public async Task<IActionResult> MarkAsAuthorized(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var order = await _orderService.GetOrderById(key);
        if (order == null)
            return NotFound();

        var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (paymentTransaction == null)
            return BadRequest("No payment transaction found for this order");

        paymentTransaction.AuthorizationTransactionId = DateTime.UtcNow.ToString("O");
        await _paymentTransactionService.UpdatePaymentTransaction(paymentTransaction);
        return Ok();
    }

    [HttpPost("{key}/Cancel")]
    public async Task<IActionResult> Cancel(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var order = await _orderService.GetOrderById(key);
        if (order == null)
            return NotFound();

        await _mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = true });
        return Ok();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var order = await _orderService.GetOrderById(key);
        if (order == null)
            return NotFound();

        await _mediator.Send(new DeleteOrderCommand { Order = order });
        return NoContent();
    }
}
