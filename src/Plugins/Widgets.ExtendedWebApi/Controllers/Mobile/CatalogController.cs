using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.Infrastructure;

namespace Widgets.ExtendedWebApi.Controllers.Mobile;

/// <summary>
/// Public catalog API for mobile app - no authentication required
/// </summary>
[ApiController]
[Route("api/mobile/[controller]")]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = false, GroupName = MobileApiOpenApiStartup.MobileApiGroupName)]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IPictureService _pictureService;

    public CatalogController(
        IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IPictureService pictureService)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _pictureService = pictureService;
    }

    /// <summary>
    /// Get list of categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = _categoryRepository.Table
            .Where(c => c.Published)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        var result = new List<object>();
        foreach (var c in categories)
        {
            var imageUrl = !string.IsNullOrEmpty(c.PictureId)
                ? await _pictureService.GetPictureUrl(c.PictureId, 300)
                : null;

            result.Add(new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                imageUrl,
                parentCategoryId = c.ParentCategoryId,
                displayOrder = c.DisplayOrder
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get list of products with optional filtering
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string categoryId = null,
        [FromQuery] string keywords = null,
        [FromQuery] double? priceMin = null,
        [FromQuery] double? priceMax = null,
        [FromQuery] bool featuredOnly = false,
        [FromQuery] int orderBy = 0,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20)
    {
        var query = _productRepository.Table
            .Where(p => p.Published && p.VisibleIndividually);

        // Filter by category
        if (!string.IsNullOrEmpty(categoryId))
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId));
        }

        // Search by keywords
        if (!string.IsNullOrEmpty(keywords))
        {
            var searchTerm = keywords.ToLower();
            query = query.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(searchTerm)) ||
                (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(searchTerm)) ||
                (p.Sku != null && p.Sku.ToLower().Contains(searchTerm)));
        }

        // Price filter
        if (priceMin.HasValue)
            query = query.Where(p => p.Price >= priceMin.Value);
        if (priceMax.HasValue)
            query = query.Where(p => p.Price <= priceMax.Value);

        // Featured/bestseller filter
        if (featuredOnly)
            query = query.Where(p => p.ShowOnHomePage || p.BestSeller);

        var totalCount = query.Count();

        // Ordering
        query = orderBy switch
        {
            1 => query.OrderBy(p => p.Name),
            2 => query.OrderBy(p => p.Price),
            3 => query.OrderByDescending(p => p.Price),
            4 => query.OrderByDescending(p => p.CreatedOnUtc),
            _ => query.OrderBy(p => p.DisplayOrder)
        };

        var products = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<object>();
        foreach (var p in products)
        {
            // Get primary image
            var primaryPicture = p.ProductPictures?
                .OrderBy(pp => pp.DisplayOrder)
                .FirstOrDefault();

            var imageUrl = primaryPicture != null
                ? await _pictureService.GetPictureUrl(primaryPicture.PictureId, 400)
                : await _pictureService.GetDefaultPictureUrl(400);

            result.Add(new
            {
                id = p.Id,
                name = p.Name,
                shortDescription = p.ShortDescription,
                sku = p.Sku,
                price = p.Price,
                oldPrice = p.OldPrice > p.Price ? p.OldPrice : (double?)null,
                imageUrl,
                inStock = p.StockQuantity > 0 || !p.ManageInventoryMethodId.Equals(ManageInventoryMethod.ManageStock),
                isFeatured = p.ShowOnHomePage || p.BestSeller
            });
        }

        return Ok(new
        {
            items = result,
            pageIndex,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    /// <summary>
    /// Get product details
    /// </summary>
    [HttpGet("products/{productId}")]
    public async Task<IActionResult> GetProductDetails(string productId)
    {
        var product = _productRepository.Table
            .FirstOrDefault(p => p.Id == productId && p.Published);

        if (product == null)
            return NotFound(new { error = "Product not found" });

        // Get all images
        var images = new List<string>();
        if (product.ProductPictures != null)
        {
            foreach (var pp in product.ProductPictures.OrderBy(x => x.DisplayOrder))
            {
                var url = await _pictureService.GetPictureUrl(pp.PictureId, 800);
                if (!string.IsNullOrEmpty(url))
                    images.Add(url);
            }
        }

        if (images.Count == 0)
        {
            images.Add(await _pictureService.GetDefaultPictureUrl(800));
        }

        // Get category names
        var categoryIds = product.ProductCategories?.Select(pc => pc.CategoryId).ToList() ?? new List<string>();
        var categories = _categoryRepository.Table
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToList();

        return Ok(new
        {
            id = product.Id,
            name = product.Name,
            shortDescription = product.ShortDescription,
            fullDescription = product.FullDescription,
            sku = product.Sku,
            price = product.Price,
            oldPrice = product.OldPrice > product.Price ? product.OldPrice : (double?)null,
            images,
            inStock = product.StockQuantity > 0 || !product.ManageInventoryMethodId.Equals(ManageInventoryMethod.ManageStock),
            stockQuantity = product.StockQuantity,
            isFeatured = product.ShowOnHomePage || product.BestSeller,
            categories
        });
    }

    /// <summary>
    /// Get featured products for home page
    /// </summary>
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10)
    {
        var products = _productRepository.Table
            .Where(p => p.Published && p.VisibleIndividually && (p.ShowOnHomePage || p.BestSeller))
            .OrderBy(p => p.DisplayOrder)
            .Take(limit)
            .ToList();

        var result = new List<object>();
        foreach (var p in products)
        {
            var primaryPicture = p.ProductPictures?
                .OrderBy(pp => pp.DisplayOrder)
                .FirstOrDefault();

            var imageUrl = primaryPicture != null
                ? await _pictureService.GetPictureUrl(primaryPicture.PictureId, 400)
                : await _pictureService.GetDefaultPictureUrl(400);

            result.Add(new
            {
                id = p.Id,
                name = p.Name,
                shortDescription = p.ShortDescription,
                price = p.Price,
                oldPrice = p.OldPrice > p.Price ? p.OldPrice : (double?)null,
                imageUrl
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Search products
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string q,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { items = new List<object>() });

        return await GetProducts(keywords: q, pageSize: pageSize);
    }
}
