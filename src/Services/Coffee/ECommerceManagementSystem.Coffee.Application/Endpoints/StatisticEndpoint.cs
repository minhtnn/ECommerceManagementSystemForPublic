using Carter;
using ECommerceManagementSystem.Coffee.Application.Features.Statics.Query.GetAllProductsStaticByBrand;
using ECommerceManagementSystem.Coffee.Application.Features.Statics.Query.GetAllPromotionRulesStaticByBrand;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class StatisticEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Statistics.StatisticsEndpoint)
            .WithTags(ApiEndpointConstants.Statistics.Tag);
        group.MapGet(ApiEndpointConstants.Statistics.GetProductsSaleStatistics, GetProductsSaleStatistics)
            .RequireAuthorization(EPolicy.SystemOrBrandPolicy.GetDisplayName())
            .WithName(nameof(GetProductsSaleStatistics))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Statistics.GetPromotionRulesSaleStatistics, GetPromotionRulesSaleStatistics)
            .RequireAuthorization(EPolicy.SystemOrBrandPolicy.GetDisplayName())
            .WithName(nameof(GetPromotionRulesSaleStatistics))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }
    
    public async Task<IResult> GetProductsSaleStatistics(
        IMediator mediator, 
        [FromQuery] int page = 1, 
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, 
        [FromQuery] bool isAsc = true)
    {
        var query = new GetAllProductsStaticByBrandQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> GetPromotionRulesSaleStatistics(
        IMediator mediator, 
        [FromQuery] int page = 1, 
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, 
        [FromQuery] bool isAsc = true)
    {
        var query = new GetAllPromotionRulesStaticByBrandQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
}