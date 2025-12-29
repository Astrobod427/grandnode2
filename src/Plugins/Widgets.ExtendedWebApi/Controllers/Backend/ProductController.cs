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

    /// <summary>
    /// Advanced product search with multiple filters
    /// </summary>
    /// <param name="keywords">Search keywords (searches in name by default)</param>
    /// <param name="searchDescriptions">Include descriptions in search</param>
    /// <param name="searchSku">Include SKU in search</param>
    /// <param name="searchProductTags">Include product tags in search</param>
    /// <param name="categoryIds">Filter by category IDs (comma-separated)</param>
    /// <param name="brandId">Filter by brand ID</param>
    /// <param name="vendorId">Filter by vendor ID</param>
    /// <param name="priceMin">Minimum price filter</param>
    /// <param name="priceMax">Maximum price filter</param>
    /// <param name="showOnHomePage">Filter products shown on home page</param>
    /// <param name="featuredProducts">Filter featured products</param>
    /// <param name="markedAsNewOnly">Show only new products</param>
    /// <param name="publishedOnly">Show only published products</param>
    /// <param name="orderBy">Sort order: 0=Position, 1=Name, 2=Price, 3=CreatedOn, 4=BestSellers, 5=OnSale</param>
    /// <param name="pageIndex">Page number (0-based)</param>
    /// <param name="pageSize">Items per page</param>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string keywords = "",
        [FromQuery] bool searchDescriptions = false,
        [FromQuery] bool searchSku = true,
        [FromQuery] bool searchProductTags = false,
        [FromQuery] string categoryIds = "",
        [FromQuery] string brandId = "",
        [FromQuery] string vendorId = "",
        [FromQuery] double? priceMin = null,
        [FromQuery] double? priceMax = null,
        [FromQuery] bool? showOnHomePage = null,
        [FromQuery] bool? featuredProducts = null,
        [FromQuery] bool markedAsNewOnly = false,
        [FromQuery] bool publishedOnly = true,
        [FromQuery] int orderBy = 0,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Forbid();

        // Parse category IDs if provided
        IList<string> categoryIdsList = null;
        if (!string.IsNullOrEmpty(categoryIds))
        {
            categoryIdsList = categoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        var (products, _) = await _productService.SearchProducts(
            keywords: keywords,
            searchDescriptions: searchDescriptions,
            searchSku: searchSku,
            searchProductTags: searchProductTags,
            categoryIds: categoryIdsList,
            brandId: brandId,
            vendorId: vendorId,
            priceMin: priceMin,
            priceMax: priceMax,
            showOnHomePage: showOnHomePage,
            featuredProducts: featuredProducts,
            markedAsNewOnly: markedAsNewOnly,
            overridePublished: publishedOnly ? false : (bool?)null,
            orderBy: (Grand.Domain.Catalog.ProductSortingEnum)orderBy,
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: !publishedOnly
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

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount = products.TotalCount,
            searchCriteria = new
            {
                keywords,
                searchDescriptions,
                searchSku,
                searchProductTags,
                categoryIds,
                brandId,
                vendorId,
                priceMin,
                priceMax,
                orderBy
            }
        });
    }
}
