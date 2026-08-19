using Carter;
using ECommerceManagementSystem.Coffee.Application.Features.Menus.Query.GetMenuByBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Menus.Query.GetPublicMenuByBrand;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class ProductMenuEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Menu.MenusEndpoint)
            .WithTags(ApiEndpointConstants.Menu.Tag);
        group.MapGet(ApiEndpointConstants.Menu.GetMenus, GetMenuByBrand)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetMenuByBrand))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Menu.GetPublicMenus, GetPublicMenuByBrand)
            // .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetPublicMenuByBrand))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    public async Task<IResult> GetPublicMenuByBrand(
        IMediator mediator,
        [FromRoute] string brandCode,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? productsSortBy = null,
        [FromQuery] bool productsIsAsc = true,
        [FromQuery] string? productName = null)
    {
        var query = new GetPublicMenuByBrandQuery
        {
            BrandCode = brandCode,
            CategoryId = categoryId,
            Page = page,
            Size = size,
            ProductsSortBy = productsSortBy,
            ProductsIsAsc = productsIsAsc,
            ProductName = productName
        };

        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetMenuByBrand(IMediator mediator, [FromQuery] Guid? categoryId)
    {
        var query = new GetMenuByBrandQuery()
        {
            CategoryId = categoryId
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
}