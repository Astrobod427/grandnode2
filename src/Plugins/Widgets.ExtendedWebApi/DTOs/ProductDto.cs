namespace Widgets.ExtendedWebApi.DTOs;

public class ProductDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public string Sku { get; set; }
    public string Gtin { get; set; }
    public string BrandId { get; set; }
    public string VendorId { get; set; }
    public double Price { get; set; }
    public double OldPrice { get; set; }
    public double CatalogPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool Published { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool BestSeller { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
}
