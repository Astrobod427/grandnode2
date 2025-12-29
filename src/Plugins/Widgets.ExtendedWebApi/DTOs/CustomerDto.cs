namespace Widgets.ExtendedWebApi.DTOs;

public class CustomerDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public bool Active { get; set; }
    public bool Deleted { get; set; }
    public bool IsSystemAccount { get; set; }
    public string StoreId { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime LastActivityDateUtc { get; set; }
}
