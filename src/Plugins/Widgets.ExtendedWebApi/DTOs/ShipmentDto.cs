namespace Widgets.ExtendedWebApi.DTOs;

public class ShipmentDto
{
    public string Id { get; set; }
    public int ShipmentNumber { get; set; }
    public string OrderId { get; set; }
    public string TrackingNumber { get; set; }
    public double? TotalWeight { get; set; }
    public DateTime? ShippedDateUtc { get; set; }
    public DateTime? DeliveryDateUtc { get; set; }
    public string AdminComment { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
