using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.CreateProductCategory;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.UpdateProductCategory;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategories;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategoryById;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;


namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class ProductCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.ProductCategory.ProductCategoriesEndpoint)
            .WithTags(ApiEndpointConstants.ProductCategory.Tag);
        group.MapGet(ApiEndpointConstants.ProductCategory.GetProductCategories, GetProductCategoriesByBrand)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetProductCategoriesByBrand))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.ProductCategory.GetProductCategoryById, GetProductCategoryById)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetProductCategoryById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.ProductCategory.CreateProductCategory, CreateCategory)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(CreateCategory))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.ProductCategory.UpdateProductCategory, UpdateCategory)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(UpdateCategory))
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }
    
    public async Task<IResult> GetProductCategoriesByBrand(
        IMediator mediator, 
        [FromQuery] int page = 1, 
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, 
        [FromQuery] bool isAsc = true, 
        [FromQuery] string? code = null, 
        [FromQuery] string? name = null, 
        [FromQuery] ECategoryStatus? status = null, 
        [FromQuery] bool? isLeafOnly = null)
    {
        var query = new GetProductCategoriesQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Name = name,
            Status= status,
            IsLeafOnly = isLeafOnly
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> GetProductCategoryById(IMediator mediator, [FromRoute] Guid id, [FromQuery] string timeZone = null)
    {
        var query = new GetProductCategoryByIdQuery()
        {
            Id = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdateCategory(IMediator mediator, [FromRoute] Guid id, [FromForm] UpdateProductCategoryCommand request, ValidationUtil<UpdateProductCategoryCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }
        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> CreateCategory(IMediator mediator, [FromForm] CreateProductCategoryCommand command, ValidationUtil<CreateProductCategoryCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }
        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }
}