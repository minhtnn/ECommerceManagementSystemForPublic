using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.CreateBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.UpdateBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrandById;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrandDetails;
using ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrands;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class BrandEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Brand.BrandsEndpoint)
            .WithTags(ApiEndpointConstants.Brand.Tag);
        group.MapGet(ApiEndpointConstants.Brand.GetBrands, GetBrands)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .WithName(nameof(GetBrands))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Brand.GetBrandById, GetBrandById)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .WithName(nameof(GetBrandById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Brand.GetBrandDetails, GetBrandDetails)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetBrandDetails))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Brand.CreateBrand, CreateBrand)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(CreateBrand))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.Brand.UpdateBrand, UpdateBrand)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .WithName(nameof(UpdateBrand))
            .DisableAntiforgery()
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    public async Task<IResult> GetBrands(IMediator mediator, [FromQuery] int page = 1, [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, [FromQuery] bool isAsc = true, [FromQuery] string? code = null,
        [FromQuery] string? name = null,
        [FromQuery] EBrandStatus? status = null)
    {
        var query = new GetBrandsQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Code = code,
            Name = name,
            Status = status
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandById(IMediator mediator, [FromRoute] Guid id, [FromQuery] string timeZone)
    {
        var query = new GetBrandByIdQuery()
        {
            BrandId = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandDetails(IMediator mediator, [FromQuery] string timeZone)
    {
        var query = new GetBrandDetailsQuery()
        {
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreateBrand(IMediator mediator, [FromForm] CreateBrandCommand command,
        ValidationUtil<CreateBrandCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdateBrand(IMediator mediator, [FromRoute] Guid id,
        [FromForm] UpdateBrandCommand request, ValidationUtil<UpdateBrandCommand> validationUtil)
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