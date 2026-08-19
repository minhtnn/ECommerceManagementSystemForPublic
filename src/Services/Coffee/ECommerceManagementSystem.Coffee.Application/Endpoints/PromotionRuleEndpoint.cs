using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.CreateBrandPromotionRule;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.UpdateBrandPromotionRule;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetApplicablePromotions;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPromotionRule;
using ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPromotionRuleById;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class PromotionRuleEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.PromotionRule.PromotionRulesEndpoint)
            .WithTags(ApiEndpointConstants.PromotionRule.Tag);
        group.MapGet(ApiEndpointConstants.PromotionRule.GetPromotionRules, GetPromotionRulesByBrand)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetPromotionRulesByBrand))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.PromotionRule.GetPromotionRuleById, GetPromotionRuleById)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetPromotionRuleById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.PromotionRule.GetApplicablePromotionRules, GetApplicablePromotionRules)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(GetApplicablePromotionRules))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.PromotionRule.CreatePromotionRule, CreatePromotionRule)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(CreatePromotionRule))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.PromotionRule.UpdatePromotionRule, UpdatePromotionRule)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(UpdatePromotionRule))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    public async Task<IResult> GetPromotionRulesByBrand(IMediator mediator, [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, [FromQuery] bool isAsc = true, [FromQuery] string? code = null,
        [FromQuery] string? name = null,
        [FromQuery] EPromotionStatus? status = null,
        [FromQuery] string timeZone = null)
    {
        var query = new GetBrandPromotionRulesQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Code = code,
            Name = name,
            Status = status,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetPromotionRuleById(IMediator mediator, [FromRoute] Guid id,
        [FromQuery] string timeZone = null)
    {
        var query = new GetBrandPromotionRuleByIdQuery()
        {
            Id = id,
            TimeZone = timeZone,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
    
    public async Task<IResult> GetApplicablePromotionRules(IMediator mediator, [FromRoute] string brandCode)
    {
        var query = new GetApplicablePromotionsQuery()
        {
            BrandCode = brandCode
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreatePromotionRule(IMediator mediator,
        [FromBody] CreateBrandPromotionRuleCommand command,
        ValidationUtil<CreateBrandPromotionRuleCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdatePromotionRule(IMediator mediator, [FromRoute] Guid id,
        [FromBody] UpdateBrandPromotionRuleCommand request,
        ValidationUtil<UpdateBrandPromotionRuleCommand> validationUtil)
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