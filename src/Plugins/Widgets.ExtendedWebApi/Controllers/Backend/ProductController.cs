using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Backend;

public class ProductController : BaseBackendApiController
{
    private readonly IProductService _productService;
    private readonly IPermissionService _permissionService;

    public ProductController(
        IProductService productService,
        IPermissionService permissionService)
    {
        _productService = productService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 50)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Forbid();

        var (products, _) = await _productService.SearchProducts(
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: true
        );

        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            FullDescription = p.FullDescription,
            Sku = p.Sku,
            Gtin = p.Gtin,
            BrandId = p.BrandId,
            VendorId = p.VendorId,
            Price = p.Price,
            OldPrice = p.OldPrice,
            CatalogPrice = p.CatalogPrice,
            StockQuantity = p.StockQuantity,
            Published = p.Published,
            ShowOnHomePage = p.ShowOnHomePage,
            BestSeller = p.BestSeller,
            CreatedOnUtc = p.CreatedOnUtc,
            UpdatedOnUtc = p.UpdatedOnUtc
        });

        return Ok(new {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount = products.TotalCount
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Forbid();

        var product = await _productService.GetProductById(id);
        if (product == null)
            return NotFound();

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Sku = product.Sku,
            Gtin = product.Gtin,
            BrandId = product.BrandId,
            VendorId = product.VendorId,
            Price = product.Price,
            OldPrice = product.OldPrice,
            CatalogPrice = product.CatalogPrice,
            StockQuantity = product.StockQuantity,
            Published = product.Published,
            ShowOnHomePage = product.ShowOnHomePage,
            BestSeller = product.BestSeller,
            CreatedOnUtc = product.CreatedOnUtc,
            UpdatedOnUtc = product.UpdatedOnUtc
        };

        return Ok(dto);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string keywords = "",
        [FromQuery] string sku = "",
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Forbid();

        var (products, _) = await _productService.SearchProducts(
            keywords: keywords,
            searchSku: !string.IsNullOrEmpty(sku),
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: true
        );

        var dtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Sku = p.Sku,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Published = p.Published
        });

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount = products.TotalCount
        });
    }
}
