using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;
using Widgets.ExtendedWebApi.Infrastructure;

namespace Widgets.ExtendedWebApi.Controllers;

/// <summary>
/// Public API endpoints for n8n and external integrations
/// Protected by API Key authentication
/// </summary>
[ApiController]
[Route("api/extended")]
[AllowAnonymous]
[ApiKeyAuthorize]
public class PublicApiController : ControllerBase
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Order> _orderRepository;

    public PublicApiController(
        IRepository<Product> productRepository,
        IRepository<Order> orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    [HttpGet("products")]
    public IActionResult ListProducts([FromQuery] int pageSize = 100)
    {
        var products = _productRepository.Table
            .Take(pageSize)
            .ToList();

        var totalCount = _productRepository.Table.Count();

        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name ?? "[NULL]",
            ShortDescription = p.ShortDescription ?? "[NULL]",
            Sku = p.Sku ?? "[NULL]",
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Published = p.Published,
            CreatedOnUtc = p.CreatedOnUtc
        }).ToList();

        return Ok(new
        {
            items = dtos,
            totalCount
        });
    }

    [HttpGet("products/search")]
    public IActionResult SearchProducts(
        [FromQuery] string keywords = "",
        [FromQuery] bool searchDescriptions = false,
        [FromQuery] bool searchSku = true,
        [FromQuery] double? priceMin = null,
        [FromQuery] double? priceMax = null,
        [FromQuery] bool publishedOnly = true,
        [FromQuery] int orderBy = 0,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        var query = _productRepository.Table.AsQueryable();

        if (!string.IsNullOrEmpty(keywords))
        {
            var searchTerm = keywords.ToLower();
            query = query.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (searchDescriptions && p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)) ||
                (searchDescriptions && p.FullDescription != null && p.FullDescription.ToLower().Contains(searchTerm)) ||
                (searchSku && p.Sku != null && p.Sku.ToLower().Contains(searchTerm))
            );
        }

        if (priceMin.HasValue)
            query = query.Where(p => p.Price >= priceMin.Value);
        if (priceMax.HasValue)
            query = query.Where(p => p.Price <= priceMax.Value);

        if (publishedOnly)
            query = query.Where(p => p.Published);

        var totalCount = query.Count();

        query = orderBy switch
        {
            1 => query.OrderBy(p => p.Name),
            2 => query.OrderBy(p => p.Price),
            3 => query.OrderByDescending(p => p.CreatedOnUtc),
            _ => query.OrderBy(p => p.DisplayOrder)
        };

        var products = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Sku = p.Sku,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Published = p.Published,
            CreatedOnUtc = p.CreatedOnUtc
        });

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount
        });
    }

    [HttpGet("orders")]
    public IActionResult ListOrders([FromQuery] int pageSize = 100)
    {
        var orders = _orderRepository.Table
            .OrderByDescending(o => o.CreatedOnUtc)
            .Take(pageSize)
            .ToList();

        var totalCount = _orderRepository.Table.Count();

        var dtos = orders.Select(o => new
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            OrderGuid = o.OrderGuid,
            CustomerId = o.CustomerId,
            CustomerEmail = o.CustomerEmail,
            OrderTotal = o.OrderTotal,
            OrderStatus = o.OrderStatusId.ToString(),
            PaymentStatus = o.PaymentStatusId.ToString(),
            ShippingStatus = o.ShippingStatusId.ToString(),
            CreatedOnUtc = o.CreatedOnUtc,
            UpdatedOnUtc = o.UpdatedOnUtc
        });

        return Ok(new
        {
            items = dtos,
            totalCount
        });
    }
}
