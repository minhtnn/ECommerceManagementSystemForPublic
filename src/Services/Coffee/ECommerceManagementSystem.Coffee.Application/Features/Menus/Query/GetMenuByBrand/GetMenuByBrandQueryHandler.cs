using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Menus;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Menus.Query.GetMenuByBrand;

public class GetMenuByBrandQueryHandler :IRequestHandler<GetMenuByBrandQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IMediaService _mediaService;
    private readonly IClaimService _claimService;

    public GetMenuByBrandQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger, IMediaService mediaService, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediaService = mediaService;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(GetMenuByBrandQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var brandId = _claimService.GetCurrentReferenceId();

        if (role == null || role != ERole.BrandAdmin || brandId == null || brandId == Guid.Empty)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        var allCategories = await _unitOfWork.GetRepository<Domain.Entities.ProductCategories>()
            .GetListAsync<GetMenuProductCategoryResponse>(
                predicate: x => x.BrandId == brandId && x.Status == ECategoryStatus.Active,
                include: x =>
                    x.Include(cat =>
                            Enumerable.Where<Domain.Entities.Products>(cat.Products,
                                p => p.Status == EProductStatus.Active))
                        .ThenInclude(p => p.ProductSideAttributes)
                        .Include(cat =>
                            Enumerable.Where<Domain.Entities.Products>(cat.Products,
                                p => p.Status == EProductStatus.Active))
                        .ThenInclude(p => p.ProductImages)
                ,
                orderBy: x => x.OrderBy(cat => cat.DisplayOrder).ThenBy(cat => cat.Name)
            );
        if (!allCategories.Any())
        {
            return new ApiResponse
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Thương hiệu này hiện không có sản phẩm nào!"
            };
        }

        foreach (var categoryResponse in allCategories)
        {
            if (categoryResponse.ImagePath != null && !string.IsNullOrEmpty(categoryResponse.ImagePath))
            {
                try
                {
                    categoryResponse.ImageUrl = await _mediaService.GetImageUrlAsync(
                        categoryResponse.ImagePath,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Warning(
                        "Failed to generate signed URL for image {ImageUrl}: {Error}",
                        categoryResponse.ImagePath,
                        ex.Message
                    );
                }
            }
        }

        List<Guid> targetCategoryIds;
        GetMenuProductCategoryResponse? selectedCategory = null;

        if (request.CategoryId.HasValue)
        {
            selectedCategory = allCategories.FirstOrDefault(x => x.Id == request.CategoryId);

            if (selectedCategory == null)
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Lấy menu thương hiệu thành công!",
                    Data = new GetBrandMenuResponse
                    {
                        SelectedCategory = null,
                        ProductCategoriesTree = BuildCategoryTree(allCategories, null),
                        Products = new List<GetMenuProductResponse>(),
                        TotalProducts = 0
                    }
                };
            }

            targetCategoryIds = GetCategoryAndDescendantIds(allCategories, request.CategoryId.Value);
        }
        else
        {
            targetCategoryIds = allCategories.Select(c => c.Id).ToList();
        }

        var products = allCategories
            .Where(x => targetCategoryIds.Contains(x.Id))
            .SelectMany(c => c.GetMenuProductsResponse ?? new List<GetMenuProductResponse>())
            .Where(p => p.Status == EProductStatus.Active)
            .Select(p => new GetMenuProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                FullName = p.FullName,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                Images = p.Images.Select(pi => new GetMenuProductImageResponse
                {
                    Id = pi.Id,
                    Path = pi.Path,
                    AltText = pi.AltText,
                    IsMainImage = pi.IsMainImage,
                }).OrderByDescending(x => x.IsMainImage).ToList()
            })
            .OrderBy(p => p.Name)
            .ToList();

        var categoryTree = BuildCategoryTree(allCategories, request.CategoryId);

        foreach (var product in products)
        {
            if (product.Images != null && product.Images.Any())
            {
                foreach (var image in product.Images)
                {
                    if (image.Path != null && !string.IsNullOrEmpty(image.Path))
                    {
                        try
                        {
                            image.Url = await _mediaService.GetImageUrlAsync(
                                image.Path,
                                TimeSpan.FromHours(1)
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(
                                "Failed to generate signed URL for image {ImageUrl}: {Error}",
                                image.Path,
                                ex.Message
                            );
                        }
                    }
                }
            }
        }


        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy menu thương hiệu thành công!",
            Data = new GetBrandMenuResponse
            {
                SelectedCategory = selectedCategory != null
                    ? MapToCategory(selectedCategory, allCategories, request.CategoryId)
                    : null,
                ProductCategoriesTree = categoryTree,
                Products = products,
                TotalProducts = products.Count
            }
        };
    }

    private List<GetMenuProductCategoryResponse> BuildCategoryTree(
        ICollection<Domain.Entities.ProductCategories> allCategories,
        Guid? selectedCategoryId)
    {
        var rootCategories = allCategories
            .Where(c => c.ParentProductCategoryId == null).ToList();
        return rootCategories.Select(c => MapToCategory(c, allCategories, selectedCategoryId)).ToList();
    }

    private GetMenuProductCategoryResponse MapToCategory(Domain.Entities.ProductCategories category,
        ICollection<Domain.Entities.ProductCategories> allCategories,
        Guid? selectedCategoryId)
    {
        var children = allCategories
            .Where(c => c.ParentProductCategoryId == category.Id)
            .Select(c => MapToCategory(c, allCategories, selectedCategoryId))
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        var directProductCount = category.Products?.Count(p => p.Status == EProductStatus.Active) ?? 0;
        var totalProductCount = directProductCount + children.Sum(c => c.TotalProductCount);

        return new GetMenuProductCategoryResponse
        {
            Id = category.Id,
            ParentProductCategoryId = category.ParentProductCategoryId ?? Guid.Empty,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            IsSelected = category.Id == selectedCategoryId,
            ProductCount = directProductCount,
            TotalProductCount = totalProductCount,
            Children = children
        };
    }

    private List<Guid> GetCategoryAndDescendantIds(ICollection<Domain.Entities.ProductCategories> allCategories,
        Guid categoryId)
    {
        var ids = new List<Guid> { categoryId };

        var children = allCategories.Where(c => c.ParentProductCategoryId == categoryId).ToList();

        foreach (var child in children)
        {
            ids.AddRange(GetCategoryAndDescendantIds(allCategories, child.Id));
        }

        return ids;
    }

    private List<GetMenuProductCategoryResponse> BuildCategoryTree(
        ICollection<GetMenuProductCategoryResponse> allCategories,
        Guid? selectedCategoryId)
    {
        var rootCategories = allCategories
            .Where(c => (c.ParentProductCategoryId == null) || (c.ParentProductCategoryId == Guid.Empty)).ToList();
        return rootCategories.Select(c => MapToCategory(c, allCategories, selectedCategoryId)).ToList();
    }

    private GetMenuProductCategoryResponse MapToCategory(GetMenuProductCategoryResponse category,
        ICollection<GetMenuProductCategoryResponse> allCategories,
        Guid? selectedCategoryId)
    {
        var children = allCategories
            .Where(c => c.ParentProductCategoryId == category.Id)
            .Select(c => MapToCategory(c, allCategories, selectedCategoryId))
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        var directProductCount = category.GetMenuProductsResponse?.Count(p => p.Status == EProductStatus.Active) ?? 0;
        var totalProductCount = directProductCount + children.Sum(c => c.TotalProductCount);
        category.Children = children;
        category.IsSelected = category.Id == selectedCategoryId;
        category.ProductCount = directProductCount;
        category.TotalProductCount = totalProductCount;
        return category;
    }

    private List<Guid> GetCategoryAndDescendantIds(ICollection<GetMenuProductCategoryResponse> allCategories,
        Guid categoryId)
    {
        var ids = new List<Guid> { categoryId };

        var children = allCategories.Where(c => c.ParentProductCategoryId == categoryId).ToList();

        foreach (var child in children)
        {
            ids.AddRange(GetCategoryAndDescendantIds(allCategories, child.Id));
        }

        return ids;
    }
}