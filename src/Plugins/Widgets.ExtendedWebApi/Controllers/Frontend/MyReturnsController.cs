using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Frontend;

public class MyReturnsController : BaseFrontendApiController
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IContextAccessor _contextAccessor;

    public MyReturnsController(
        IMerchandiseReturnService merchandiseReturnService,
        IContextAccessor contextAccessor)
    {
        _merchandiseReturnService = merchandiseReturnService;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var returnsPage = await _merchandiseReturnService.SearchMerchandiseReturns(customerId: customer.Id);
        var dtos = returnsPage.Select(r => new MerchandiseReturnDto
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            OrderId = r.OrderId,
            CustomerId = r.CustomerId,
            CustomerComments = r.CustomerComments,
            StaffNotes = r.StaffNotes,
            MerchandiseReturnStatus = r.MerchandiseReturnStatus.ToString(),
            PickupDate = r.PickupDate,
            CreatedOnUtc = r.CreatedOnUtc
        });

        return Ok(dtos);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        if (customer == null || string.IsNullOrEmpty(customer.Email))
            return Unauthorized();

        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(key);
        if (merchandiseReturn == null || merchandiseReturn.CustomerId != customer.Id)
            return NotFound();

        var dto = new MerchandiseReturnDto
        {
            Id = merchandiseReturn.Id,
            ReturnNumber = merchandiseReturn.ReturnNumber,
            OrderId = merchandiseReturn.OrderId,
            CustomerId = merchandiseReturn.CustomerId,
            CustomerComments = merchandiseReturn.CustomerComments,
            StaffNotes = merchandiseReturn.StaffNotes,
            MerchandiseReturnStatus = merchandiseReturn.MerchandiseReturnStatus.ToString(),
            PickupDate = merchandiseReturn.PickupDate,
            CreatedOnUtc = merchandiseReturn.CreatedOnUtc
        };

        return Ok(dto);
    }
}
