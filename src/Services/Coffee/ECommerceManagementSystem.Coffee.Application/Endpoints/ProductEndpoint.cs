using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategories;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Command.CreateProduct;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Command.UpdateProduct;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Query.GetProductById;
using ECommerceManagementSystem.Coffee.Application.Features.Products.Query.GetProducts;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class ProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Product.ProductsEndpoint)
            .WithTags(ApiEndpointConstants.Product.Tag);
        group.MapGet(ApiEndpointConstants.Product.GetProducts, GetProductsByBrand)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetProductsByBrand))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Product.GetProductById, GetProductById)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetProductById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Product.GetPublicProductById, GetPublicProductById)
            .WithName(nameof(GetPublicProductById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Product.CreateProduct, CreateProduct)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(CreateProduct))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.Product.UpdateProduct, UpdateProduct)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(UpdateProduct))
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    public async Task<IResult> GetProductsByBrand(IMediator mediator, [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, [FromQuery] bool isAsc = true, [FromQuery] string? name = null,
        [FromQuery] EProductStatus? status = null)
    {
        var query = new GetProductsQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Name = name,
            Status = status
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetProductById(IMediator mediator, [FromRoute] Guid id,
        [FromQuery] string timeZone = null)
    {
        var query = new GetProductByIdQuery()
        {
            ProductId = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetPublicProductById(IMediator mediator, [FromRoute] string brandCode,
        [FromRoute] Guid id, [FromQuery] string timeZone = null)
    {
        var query = new GetPublicProductByIdQuery()
        {
            BrandCode = brandCode,
            ProductId = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreateProduct(IMediator mediator, [FromForm] CreateProductCommand command,
        ValidationUtil<CreateProductCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdateProduct(IMediator mediator, [FromRoute] Guid id,
        [FromForm] UpdateProductCommand request, ValidationUtil<UpdateProductCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }
}