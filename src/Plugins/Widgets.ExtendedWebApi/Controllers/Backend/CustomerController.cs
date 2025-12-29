using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Backend;

public class CustomerController : BaseBackendApiController
{
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;

    public CustomerController(
        ICustomerService customerService,
        IPermissionService permissionService)
    {
        _customerService = customerService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string email = null,
        [FromQuery] string username = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageCustomers))
            return Forbid();

        var customers = await _customerService.GetAllCustomers(
            email: email,
            username: username,
            pageIndex: pageIndex,
            pageSize: pageSize
        );

        var dtos = customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            Email = c.Email,
            Username = c.Username,
            Active = c.Active,
            Deleted = c.Deleted,
            IsSystemAccount = c.IsSystemAccount,
            StoreId = c.StoreId,
            CreatedOnUtc = c.CreatedOnUtc,
            LastActivityDateUtc = c.LastActivityDateUtc
        });

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount = customers.TotalCount
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageCustomers))
            return Forbid();

        var customer = await _customerService.GetCustomerById(id);
        if (customer == null)
            return NotFound();

        var dto = new CustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            Username = customer.Username,
            Active = customer.Active,
            Deleted = customer.Deleted,
            IsSystemAccount = customer.IsSystemAccount,
            StoreId = customer.StoreId,
            CreatedOnUtc = customer.CreatedOnUtc,
            LastActivityDateUtc = customer.LastActivityDateUtc
        };

        return Ok(dto);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string email = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageCustomers))
            return Forbid();

        var customers = await _customerService.GetAllCustomers(
            email: email,
            pageIndex: pageIndex,
            pageSize: pageSize
        );

        var dtos = customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            Email = c.Email,
            Username = c.Username,
            Active = c.Active,
            StoreId = c.StoreId
        });

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount = customers.TotalCount
        });
    }
}
