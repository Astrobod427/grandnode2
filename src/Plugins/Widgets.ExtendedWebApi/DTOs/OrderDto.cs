namespace Widgets.ExtendedWebApi.DTOs;

public class OrderDto
{
    public string Id { get; set; }
    public int OrderNumber { get; set; }
    public string CustomerId { get; set; }
    public string CustomerEmail { get; set; }
    public double OrderTotal { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentStatus { get; set; }
    public string ShippingStatus { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? PaidDateUtc { get; set; }
    public string CurrencyCode { get; set; }
}
