namespace Widgets.ExtendedWebApi.DTOs;

public class CategoryDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ParentCategoryId { get; set; }
    public string PictureId { get; set; }
    public bool Published { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool IncludeInMenu { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
}
