using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Backend;

public class MerchandiseReturnController : BaseBackendApiController
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IPermissionService _permissionService;

    public MerchandiseReturnController(
        IMerchandiseReturnService merchandiseReturnService,
        IPermissionService permissionService)
    {
        _merchandiseReturnService = merchandiseReturnService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var returnsPage = await _merchandiseReturnService.SearchMerchandiseReturns();
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
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(key);
        if (merchandiseReturn == null)
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

    [HttpPatch("{key}")]
    public async Task<IActionResult> UpdateStatus(string key, [FromBody] UpdateMerchandiseReturnRequest request)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(key);
        if (merchandiseReturn == null)
            return NotFound();

        if (request.MerchandiseReturnStatus.HasValue)
            merchandiseReturn.MerchandiseReturnStatus = request.MerchandiseReturnStatus.Value;

        if (!string.IsNullOrEmpty(request.StaffNotes))
            merchandiseReturn.StaffNotes = request.StaffNotes;

        await _merchandiseReturnService.UpdateMerchandiseReturn(merchandiseReturn);
        return Ok();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Forbid();

        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(key);
        if (merchandiseReturn == null)
            return NotFound();

        await _merchandiseReturnService.DeleteMerchandiseReturn(merchandiseReturn);
        return NoContent();
    }
}

public class UpdateMerchandiseReturnRequest
{
    public MerchandiseReturnStatus? MerchandiseReturnStatus { get; set; }
    public string StaffNotes { get; set; }
}
