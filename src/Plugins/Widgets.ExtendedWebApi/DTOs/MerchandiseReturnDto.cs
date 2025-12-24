namespace Widgets.ExtendedWebApi.DTOs;

public class MerchandiseReturnDto
{
    public string Id { get; set; }
    public int ReturnNumber { get; set; }
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string CustomerComments { get; set; }
    public string StaffNotes { get; set; }
    public string MerchandiseReturnStatus { get; set; }
    public DateTime PickupDate { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
