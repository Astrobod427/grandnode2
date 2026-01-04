using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Widgets.ExtendedWebApi.Controllers.Mobile;

/// <summary>
/// Public catalog API for mobile app - no authentication required
/// </summary>
[ApiController]
[Route("api/mobile/[controller]")]
[AllowAnonymous]
[Produces("application/json")]
public class CatalogController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IPictureService _pictureService;

    public CatalogController(
        IProductService productService,
        ICategoryService categoryService,
        IPictureService pictureService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _pictureService = pictureService;
    }

    /// <summary>
    /// Get featured/homepage products
    /// </summary>
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10)
    {
        var productIds = await _productService.GetAllProductsDisplayedOnHomePage();
        var products = await _productService.GetProductsByIds(productIds.Take(limit).ToArray());

        var result = new List<object>();
        foreach (var product in products.Where(p => p.Published))
        {
            var imageUrl = await GetProductImageUrl(product);
            result.Add(MapProductToDto(product, imageUrl));
        }

        return Ok(new { items = result, totalCount = productIds.Count });
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllCategories(showHidden: false);

        var result = new List<object>();
        foreach (var category in categories)
        {
            var imageUrl = !string.IsNullOrEmpty(category.PictureId)
                ? await _pictureService.GetPictureUrl(category.PictureId, 200)
                : null;

            result.Add(new
            {
                id = category.Id,
                name = category.Name,
                description = category.Description,
                imageUrl,
                parentCategoryId = category.ParentCategoryId,
                displayOrder = category.DisplayOrder
            });
        }

        return Ok(new { items = result, totalCount = categories.Count });
    }

    /// <summary>
    /// Get products by category (includes subcategories)
    /// </summary>
    [HttpGet("categories/{categoryId}/products")]
    public async Task<IActionResult> GetProductsByCategory(
        string categoryId,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string orderBy = "position")
    {
        var sortOrder = orderBy?.ToLower() switch
        {
            "name" => ProductSortingEnum.NameAsc,
            "price" => ProductSortingEnum.PriceAsc,
            "price_desc" => ProductSortingEnum.PriceDesc,
            "newest" => ProductSortingEnum.CreatedOn,
            _ => ProductSortingEnum.Position
        };

        // Get the category and all its subcategories
        var categoryIds = new List<string> { categoryId };
        var allCategories = await _categoryService.GetAllCategories(showHidden: false);
        var subcategories = GetSubcategoriesRecursive(allCategories, categoryId);
        categoryIds.AddRange(subcategories.Select(c => c.Id));

        var (products, _) = await _productService.SearchProducts(
            pageIndex: pageIndex,
            pageSize: pageSize,
            categoryIds: categoryIds,
            visibleIndividuallyOnly: true,
            orderBy: sortOrder
        );

        var result = new List<object>();
        foreach (var product in products)
        {
            var imageUrl = await GetProductImageUrl(product);
            result.Add(MapProductToDto(product, imageUrl));
        }

        return Ok(new
        {
            items = result,
            pageIndex,
            pageSize,
            totalCount = products.TotalCount,
            totalPages = products.TotalPages
        });
    }

    /// <summary>
    /// Search products
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string q = "",
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] double? priceMin = null,
        [FromQuery] double? priceMax = null,
        [FromQuery] string categoryId = null,
        [FromQuery] string orderBy = "relevance")
    {
        var sortOrder = orderBy?.ToLower() switch
        {
            "name" => ProductSortingEnum.NameAsc,
            "price" => ProductSortingEnum.PriceAsc,
            "price_desc" => ProductSortingEnum.PriceDesc,
            "newest" => ProductSortingEnum.CreatedOn,
            _ => ProductSortingEnum.Position
        };

        var categoryIds = !string.IsNullOrEmpty(categoryId)
            ? new List<string> { categoryId }
            : null;

        var (products, _) = await _productService.SearchProducts(
            pageIndex: pageIndex,
            pageSize: pageSize,
            categoryIds: categoryIds,
            keywords: q,
            searchDescriptions: true,
            searchSku: true,
            priceMin: priceMin,
            priceMax: priceMax,
            visibleIndividuallyOnly: true,
            orderBy: sortOrder
        );

        var result = new List<object>();
        foreach (var product in products)
        {
            var imageUrl = await GetProductImageUrl(product);
            result.Add(MapProductToDto(product, imageUrl));
        }

        return Ok(new
        {
            items = result,
            query = q,
            pageIndex,
            pageSize,
            totalCount = products.TotalCount,
            totalPages = products.TotalPages
        });
    }

    /// <summary>
    /// Get product details by ID
    /// </summary>
    [HttpGet("products/{productId}")]
    public async Task<IActionResult> GetProductById(string productId)
    {
        var product = await _productService.GetProductById(productId);

        if (product == null || !product.Published)
            return NotFound(new { error = "Product not found" });

        // Get all product images
        var images = new List<string>();
        foreach (var picture in product.ProductPictures.OrderBy(p => p.DisplayOrder))
        {
            var url = await _pictureService.GetPictureUrl(picture.PictureId, 600);
            if (!string.IsNullOrEmpty(url))
                images.Add(url);
        }

        var mainImageUrl = images.FirstOrDefault()
            ?? await _pictureService.GetDefaultPictureUrl(600);

        return Ok(new
        {
            id = product.Id,
            name = product.Name,
            shortDescription = product.ShortDescription,
            fullDescription = product.FullDescription,
            sku = product.Sku,
            price = product.Price,
            oldPrice = product.OldPrice > 0 ? product.OldPrice : (double?)null,
            inStock = product.StockQuantity > 0 || product.ManageInventoryMethodId == ManageInventoryMethod.DontManageStock,
            stockQuantity = product.StockQuantity,
            imageUrl = mainImageUrl,
            images,
            isFeatured = product.ShowOnHomePage,
            isNew = product.MarkAsNew,
            rating = product.ApprovedRatingSum > 0 && product.ApprovedTotalReviews > 0
                ? (double)product.ApprovedRatingSum / product.ApprovedTotalReviews
                : (double?)null,
            reviewCount = product.ApprovedTotalReviews
        });
    }

    /// <summary>
    /// Get new/marked as new products
    /// </summary>
    [HttpGet("new")]
    public async Task<IActionResult> GetNewProducts([FromQuery] int limit = 10)
    {
        var (products, _) = await _productService.SearchProducts(
            pageSize: limit,
            markedAsNewOnly: true,
            visibleIndividuallyOnly: true,
            orderBy: ProductSortingEnum.CreatedOn
        );

        var result = new List<object>();
        foreach (var product in products)
        {
            var imageUrl = await GetProductImageUrl(product);
            result.Add(MapProductToDto(product, imageUrl));
        }

        return Ok(new { items = result, totalCount = products.TotalCount });
    }

    /// <summary>
    /// Get best sellers
    /// </summary>
    [HttpGet("bestsellers")]
    public async Task<IActionResult> GetBestSellers([FromQuery] int limit = 10)
    {
        var productIds = await _productService.GetAllProductsDisplayedOnBestSeller();
        var products = await _productService.GetProductsByIds(productIds.Take(limit).ToArray());

        var result = new List<object>();
        foreach (var product in products.Where(p => p.Published))
        {
            var imageUrl = await GetProductImageUrl(product);
            result.Add(MapProductToDto(product, imageUrl));
        }

        return Ok(new { items = result, totalCount = productIds.Count });
    }

    #region Private helpers

    private async Task<string> GetProductImageUrl(Product product)
    {
        var picture = product.ProductPictures
            .OrderBy(p => p.DisplayOrder)
            .FirstOrDefault();

        if (picture != null)
            return await _pictureService.GetPictureUrl(picture.PictureId, 300);

        return await _pictureService.GetDefaultPictureUrl(300);
    }

    private static object MapProductToDto(Product product, string imageUrl)
    {
        return new
        {
            id = product.Id,
            name = product.Name,
            shortDescription = product.ShortDescription,
            sku = product.Sku,
            price = product.Price,
            oldPrice = product.OldPrice > 0 ? product.OldPrice : (double?)null,
            inStock = product.StockQuantity > 0 || product.ManageInventoryMethodId == ManageInventoryMethod.DontManageStock,
            imageUrl,
            isFeatured = product.ShowOnHomePage,
            isNew = product.MarkAsNew
        };
    }

    private static List<Category> GetSubcategoriesRecursive(IList<Category> allCategories, string parentCategoryId)
    {
        var result = new List<Category>();
        var directChildren = allCategories.Where(c => c.ParentCategoryId == parentCategoryId).ToList();

        foreach (var child in directChildren)
        {
            result.Add(child);
            // Recursively get subcategories of this child
            result.AddRange(GetSubcategoriesRecursive(allCategories, child.Id));
        }

        return result;
    }

    #endregion
}
