using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Integration.Ricardo.Models;
using Microsoft.Extensions.Logging;

namespace Integration.Ricardo.Services;

/// <summary>
/// Service for publishing GrandNode products to ricardo.ch
/// </summary>
public class RicardoProductService
{
    private readonly RicardoApiClient _apiClient;
    private readonly IProductService _productService;
    private readonly RicardoSettings _settings;
    private readonly ILogger<RicardoProductService> _logger;

    public RicardoProductService(
        RicardoApiClient apiClient,
        IProductService productService,
        RicardoSettings settings,
        ILogger<RicardoProductService> logger)
    {
        _apiClient = apiClient;
        _productService = productService;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Publish a single product to ricardo.ch
    /// </summary>
    public async Task<PublishProductResult> PublishProductAsync(string productId, int? categoryId = null)
    {
        try
        {
            // Get product from GrandNode
            var product = await _productService.GetProductById(productId);
            if (product == null)
            {
                return new PublishProductResult
                {
                    Success = false,
                    ErrorMessage = $"Product {productId} not found"
                };
            }

            // Validate product data
            var validation = ValidateProduct(product);
            if (!validation.IsValid)
            {
                return new PublishProductResult
                {
                    Success = false,
                    ErrorMessage = $"Product validation failed: {validation.ErrorMessage}"
                };
            }

            // Calculate price with markup
            var finalPrice = (decimal)product.Price * (1 + (_settings.PriceMarkupPercentage / 100));

            // Prepare ricardo article request
            var request = new InsertArticleRequest
            {
                CategoryId = categoryId ?? _settings.DefaultCategoryId,
                ArticleTitle = TruncateString(product.Name, 40),
                ArticleDescription = PrepareDescription(product),
                ArticleConditionId = 1, // New
                StartPrice = finalPrice,
                Availability = product.StockQuantity,
                ArticleDuration = _settings.DefaultArticleDurationDays,
                Pictures = PreparePictures(product),
                PaymentConditionIds = new PaymentConditionIds
                {
                    PaymentConditionId = new List<int> { 1, 2 } // Cash, Bank transfer
                },
                DeliveryConditionIds = new DeliveryConditionIds
                {
                    DeliveryConditionId = new List<int> { 1, 2 } // Pickup, Shipping
                },
                WarrantyConditionIds = new WarrantyConditionIds
                {
                    WarrantyConditionId = new List<int> { 2 } // Manufacturer warranty
                }
            };

            // Publish to ricardo.ch
            var response = await _apiClient.InsertArticleAsync(request);

            if (response.ArticleId > 0)
            {
                _logger.LogInformation("Successfully published product {ProductId} to ricardo.ch as article {ArticleId}",
                    productId, response.ArticleId);

                return new PublishProductResult
                {
                    Success = true,
                    RicardoArticleId = response.ArticleId,
                    RicardoArticleNr = response.ArticleNr
                };
            }

            return new PublishProductResult
            {
                Success = false,
                ErrorMessage = response.ErrorMessage ?? "Unknown error"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing product {ProductId} to ricardo.ch", productId);
            return new PublishProductResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Update stock quantity on ricardo.ch
    /// </summary>
    public async Task<bool> UpdateStockAsync(long ricardoArticleId, int newQuantity)
    {
        try
        {
            var response = await _apiClient.UpdateArticleQuantityAsync(ricardoArticleId, newQuantity);

            if (response.Success)
            {
                _logger.LogInformation("Successfully updated stock for ricardo article {ArticleId} to {Quantity}",
                    ricardoArticleId, newQuantity);
                return true;
            }

            _logger.LogError("Failed to update stock for ricardo article {ArticleId}: {Error}",
                ricardoArticleId, response.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for ricardo article {ArticleId}", ricardoArticleId);
            return false;
        }
    }

    /// <summary>
    /// Close/remove article from ricardo.ch
    /// </summary>
    public async Task<bool> CloseArticleAsync(long ricardoArticleId)
    {
        try
        {
            var response = await _apiClient.CloseArticleAsync(ricardoArticleId);

            if (response.Success)
            {
                _logger.LogInformation("Successfully closed ricardo article {ArticleId}", ricardoArticleId);
                return true;
            }

            _logger.LogError("Failed to close ricardo article {ArticleId}: {Error}",
                ricardoArticleId, response.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing ricardo article {ArticleId}", ricardoArticleId);
            return false;
        }
    }

    private ProductValidationResult ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return new ProductValidationResult { IsValid = false, ErrorMessage = "Product name is required" };
        }

        if (product.Price <= 0)
        {
            return new ProductValidationResult { IsValid = false, ErrorMessage = "Product price must be greater than 0" };
        }

        if (product.StockQuantity <= 0)
        {
            return new ProductValidationResult { IsValid = false, ErrorMessage = "Product must have stock available" };
        }

        return new ProductValidationResult { IsValid = true };
    }

    private string PrepareDescription(Product product)
    {
        var description = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(product.ShortDescription))
        {
            description.AppendLine(product.ShortDescription);
            description.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(product.FullDescription))
        {
            // Remove HTML tags (basic cleanup)
            var cleanDescription = System.Text.RegularExpressions.Regex.Replace(
                product.FullDescription,
                "<.*?>",
                string.Empty
            );
            description.AppendLine(cleanDescription);
        }

        if (!string.IsNullOrWhiteSpace(product.Sku))
        {
            description.AppendLine();
            description.AppendLine($"SKU: {product.Sku}");
        }

        return TruncateString(description.ToString(), 8000);
    }

    private List<PictureInformation> PreparePictures(Product product)
    {
        var pictures = new List<PictureInformation>();

        if (product.ProductPictures != null && product.ProductPictures.Any())
        {
            int index = 0;
            foreach (var pic in product.ProductPictures.OrderBy(p => p.DisplayOrder).Take(10))
            {
                pictures.Add(new PictureInformation
                {
                    PictureUrl = $"/content/images/thumbs/{pic.PictureId}.jpeg",
                    PictureIndex = index++
                });
            }
        }

        return pictures;
    }

    private string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }
}

public class PublishProductResult
{
    public bool Success { get; set; }
    public long RicardoArticleId { get; set; }
    public int RicardoArticleNr { get; set; }
    public string ErrorMessage { get; set; }
}

public class ProductValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
}
