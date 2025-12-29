using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Microsoft.AspNetCore.Mvc;
using Widgets.ExtendedWebApi.DTOs;

namespace Widgets.ExtendedWebApi.Controllers.Backend;

public class CategoryController : BaseBackendApiController
{
    private readonly ICategoryService _categoryService;
    private readonly IPermissionService _permissionService;

    public CategoryController(
        ICategoryService categoryService,
        IPermissionService permissionService)
    {
        _categoryService = categoryService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 50)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageCategories))
            return Forbid();

        var categories = await _categoryService.GetAllCategories(
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: true
        );

        var dtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryId,
            PictureId = c.PictureId,
            Published = c.Published,
            ShowOnHomePage = c.ShowOnHomePage,
            IncludeInMenu = c.IncludeInMenu,
            DisplayOrder = c.DisplayOrder,
            CreatedOnUtc = c.CreatedOnUtc,
            UpdatedOnUtc = c.UpdatedOnUtc
        });

        return Ok(new
        {
            items = dtos,
            pageIndex,
            pageSize,
            totalCount = categories.TotalCount
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageCategories))
            return Forbid();

        var category = await _categoryService.GetCategoryById(id);
        if (category == null)
            return NotFound();

        var dto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            PictureId = category.PictureId,
            Published = category.Published,
            ShowOnHomePage = category.ShowOnHomePage,
            IncludeInMenu = category.IncludeInMenu,
            DisplayOrder = category.DisplayOrder,
            CreatedOnUtc = category.CreatedOnUtc,
            UpdatedOnUtc = category.UpdatedOnUtc
        };

        return Ok(dto);
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageCategories))
            return Forbid();

        var categories = await _categoryService.GetAllCategoriesByParentCategoryId(
            showHidden: true,
            includeAllLevels: true
        );

        var dtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentCategoryId = c.ParentCategoryId,
            DisplayOrder = c.DisplayOrder,
            Published = c.Published
        });

        return Ok(dtos);
    }
}
