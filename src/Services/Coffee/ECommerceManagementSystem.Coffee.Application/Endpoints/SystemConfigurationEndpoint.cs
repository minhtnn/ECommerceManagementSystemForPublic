using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.CreateSystemConfig;
using ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.UpdateSystemConfig;
using ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Query.GetSystemConfigs;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class SystemConfigurationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.SystemConfiguration.SystemConfigurationEndpoint)
            .WithTags(ApiEndpointConstants.SystemConfiguration.Tag);
        group.MapGet(ApiEndpointConstants.SystemConfiguration.GetSystemConfigurations, GetSystemConfigurations)
            .RequireAuthorization(EPolicy.SystemOrBrandPolicy.GetDisplayName())
            .WithName(nameof(GetSystemConfigurations))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.SystemConfiguration.CreateSystemConfiguration, CreateSystemConfiguration)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .WithName(nameof(CreateSystemConfiguration))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.SystemConfiguration.UpdateSystemConfiguration, UpdateSystemConfiguration)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .WithName(nameof(UpdateSystemConfiguration))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    public async Task<IResult> GetSystemConfigurations(IMediator mediator)
    {
        var query = new GetSystemConfigsQuery() { };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> CreateSystemConfiguration(IMediator mediator, [FromBody] CreateSystemConfigCommand command, ValidationUtil<CreateSystemConfigCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }
        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> UpdateSystemConfiguration(IMediator mediator, [FromRoute] Guid id, [FromBody] UpdateSystemConfigCommand request, ValidationUtil<UpdateSystemConfigCommand> validationUtil)
    {
        request.Id = id;
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }
        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }
}