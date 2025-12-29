using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Areas.Admin.Controllers;

public class ExtendedWebApiController : BaseAdminPluginController
{
    private readonly IWebHostEnvironment _env;
    private readonly IProductService _productService;
    private readonly IPermissionService _permissionService;
    private readonly IRepository<Product> _productRepository;

    // API Key for n8n and external integrations
    private const string API_KEY = "labaraque-api-key-2025";

    public ExtendedWebApiController(
        IWebHostEnvironment env,
        IProductService productService,
        IPermissionService permissionService,
        IRepository<Product> productRepository)
    {
        _env = env;
        _productService = productService;
        _permissionService = permissionService;
        _productRepository = productRepository;
    }

    /// <summary>
    /// Check if request has valid API key or user has admin permissions
    /// </summary>
    private async Task<bool> IsAuthorized()
    {
        // Check for API key in header
        var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey) && apiKey == API_KEY)
            return true;

        // Fallback to permission check for admin users
        return await _permissionService.Authorize(StandardPermission.ManageProducts);
    }

    public IActionResult Configure()
    {
        return View();
    }

    public async Task<IActionResult> ApiSearchDocs()
    {
        var pluginPath = Path.Combine(_env.ContentRootPath, "Plugins", "Widgets.ExtendedWebApi");
        var filePath = Path.Combine(pluginPath, "API-SEARCH.md");

        if (!System.IO.File.Exists(filePath))
            return NotFound("API-SEARCH.md not found");

        var content = await System.IO.File.ReadAllTextAsync(filePath);
        return Content(content, "text/markdown");
    }

    /// <summary>
    /// Simple list all products endpoint for diagnostics - uses repository directly
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListAllProducts([FromQuery] int pageSize = 100)
    {

        // Query repository directly
        var products = _productRepository.Table
            .Take(pageSize)
            .ToList();

        var totalCount = _productRepository.Table.Count();

        // Ultra-detailed debug of first product
        var firstProduct = products.FirstOrDefault();
        object ultraDebug = null;

        if (firstProduct != null)
        {
            var props = firstProduct.GetType().GetProperties()
                .Where(p => new[] { "Id", "Name", "ShortDescription", "Sku", "Price", "Published", "ProductTypeId" }.Contains(p.Name))
                .ToDictionary(
                    p => p.Name,
                    p => {
                        var value = p.GetValue(firstProduct);
                        return value == null ? "NULL" :
                               value is string s ? (string.IsNullOrEmpty(s) ? "EMPTY_STRING" : s) :
                               value.ToString();
                    }
                );

            ultraDebug = new
            {
                objectType = firstProduct.GetType().FullName,
                objectHash = firstProduct.GetHashCode(),
                propertyValues = props,
                rawNameValue = firstProduct.Name,
                nameIsNull = firstProduct.Name == null,
                nameIsEmpty = string.IsNullOrEmpty(firstProduct.Name),
                nameLength = firstProduct.Name?.Length ?? -1
            };
        }

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
            totalCount,
            message = $"Repository.Table query - Found {totalCount} total products",
            ultraDebug
        });
    }

    /// <summary>
    /// Search endpoint for the interactive tester - uses repository directly with manual filtering
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> SearchProducts(
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
        // Start with all products from repository
        var query = _productRepository.Table.AsQueryable();

        // Apply keyword search
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

        // Apply price filters
        if (priceMin.HasValue)
            query = query.Where(p => p.Price >= priceMin.Value);
        if (priceMax.HasValue)
            query = query.Where(p => p.Price <= priceMax.Value);

        // Apply brand filter
        if (!string.IsNullOrEmpty(brandId))
            query = query.Where(p => p.BrandId == brandId);

        // Apply vendor filter
        if (!string.IsNullOrEmpty(vendorId))
            query = query.Where(p => p.VendorId == vendorId);

        // Apply published filter
        if (publishedOnly)
            query = query.Where(p => p.Published);

        // Apply showOnHomePage filter
        if (showOnHomePage.HasValue)
            query = query.Where(p => p.ShowOnHomePage == showOnHomePage.Value);

        // Apply featuredProducts filter
        if (featuredProducts.HasValue)
            query = query.Where(p => p.BestSeller == featuredProducts.Value);

        // Apply markedAsNewOnly filter
        if (markedAsNewOnly)
            query = query.Where(p => p.MarkAsNew);

        // Get total count before pagination
        var totalCount = query.Count();

        // Apply sorting
        query = orderBy switch
        {
            1 => query.OrderBy(p => p.Name),
            2 => query.OrderBy(p => p.Price),
            3 => query.OrderByDescending(p => p.CreatedOnUtc),
            4 => query.OrderByDescending(p => p.BestSeller),
            _ => query.OrderBy(p => p.DisplayOrder)
        };

        // Apply pagination
        var products = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

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
            totalCount,
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
