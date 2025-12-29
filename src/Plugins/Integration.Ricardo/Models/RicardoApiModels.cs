namespace Integration.Ricardo.Models;

/// <summary>
/// ricardo.ch API authentication response
/// </summary>
public class RicardoTokenCredential
{
    public string TokenCredential { get; set; }
    public string TokenExpirationDate { get; set; }
    public int SessionDuration { get; set; }
}

/// <summary>
/// ricardo.ch article insertion request
/// </summary>
public class InsertArticleRequest
{
    public int CategoryId { get; set; }
    public string ArticleTitle { get; set; }
    public string ArticleDescription { get; set; }
    public int ArticleConditionId { get; set; } = 1; // 1 = New
    public decimal StartPrice { get; set; }
    public int Availability { get; set; } = 1; // 1 = Available
    public bool IsCustomerTemplate { get; set; } = false;
    public int MaxNumberOfPictures { get; set; } = 10;
    public List<PictureInformation> Pictures { get; set; } = new();
    public int ArticleDuration { get; set; } = 7; // Days
    public PaymentConditionIds PaymentConditionIds { get; set; } = new();
    public DeliveryConditionIds DeliveryConditionIds { get; set; } = new();
    public WarrantyConditionIds WarrantyConditionIds { get; set; } = new();
}

public class PictureInformation
{
    public string PictureUrl { get; set; }
    public int PictureIndex { get; set; }
}

public class PaymentConditionIds
{
    public List<int> PaymentConditionId { get; set; } = new() { 1 }; // 1 = Cash
}

public class DeliveryConditionIds
{
    public List<int> DeliveryConditionId { get; set; } = new() { 1 }; // 1 = Pickup
}

public class WarrantyConditionIds
{
    public List<int> WarrantyConditionId { get; set; } = new() { 1 }; // 1 = No warranty
}

/// <summary>
/// ricardo.ch article insertion response
/// </summary>
public class InsertArticleResponse
{
    public long ArticleId { get; set; }
    public int ArticleNr { get; set; }
    public bool IsCustomerTemplate { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>
/// ricardo.ch update quantity request
/// </summary>
public class UpdateArticleQuantityRequest
{
    public long ArticleId { get; set; }
    public int NewQuantity { get; set; }
}

/// <summary>
/// ricardo.ch update quantity response
/// </summary>
public class UpdateArticleQuantityResponse
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>
/// ricardo.ch close article request
/// </summary>
public class CloseArticleRequest
{
    public long ArticleId { get; set; }
}

/// <summary>
/// ricardo.ch close article response
/// </summary>
public class CloseArticleResponse
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
}
