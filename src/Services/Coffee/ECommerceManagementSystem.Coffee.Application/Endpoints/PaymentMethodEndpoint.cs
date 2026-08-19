using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreateBrandPaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdateBrandPaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.UpdatePaymentMethod;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetBrandPaymentMethodById;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetBrandPaymentMethods;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPaymentMethodById;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPaymentMethods;
using ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPublicBrandPaymentMethods;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class PaymentMethodEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Payment.PaymentMethodsEndpoint)
            .WithTags(ApiEndpointConstants.Payment.Tag);

        #region System payment methods

        group.MapGet(ApiEndpointConstants.Payment.GetPaymentMethods, GetPaymentMethods)
            .RequireAuthorization(EPolicy.SystemOrBrandPolicy.GetDisplayName())
            .WithName(nameof(GetPaymentMethods))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Payment.GetPaymentMethodById, GetPaymentMethodById)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .WithName(nameof(GetPaymentMethodById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Payment.CreatePaymentMethod, CreatePaymentMethod)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(CreatePaymentMethod))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.Payment.UpdatePaymentMethod, UpdatePaymentMethod)
            .RequireAuthorization(EPolicy.SystemPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(UpdatePaymentMethod))
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

        #endregion

        #region Brand payment methods

        group.MapGet(ApiEndpointConstants.Payment.GetBrandPaymentMethods, GetBrandPaymentMethods)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetBrandPaymentMethods))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Payment.GetBrandPaymentMethodById, GetBrandPaymentMethodById)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetBrandPaymentMethodById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Payment.CreateBrandPaymentMethod, CreateBrandPaymentMethod)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(CreateBrandPaymentMethod))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.Payment.UpdateBrandPaymentMethod, UpdateBrandPaymentMethod)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(UpdateBrandPaymentMethod))
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Payment.GetBrandPublicPaymentMethods, GetBrandPublicPaymentMethods)
            .WithName(nameof(GetBrandPublicPaymentMethods))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

        #endregion
    }

    #region System payment methods

    public async Task<IResult> GetPaymentMethods(IMediator mediator, [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, [FromQuery] bool isAsc = true, [FromQuery] string? code = null,
        [FromQuery] string? name = null,
        [FromQuery] EPaymentMethodStatus? status = null)
    {
        var query = new GetPaymentMethodsQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Name = name,
            Status = status,
            Code = code,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetPaymentMethodById(IMediator mediator, [FromRoute] Guid id)
    {
        var query = new GetPaymentMethodByIdQuery()
        {
            PaymentMethodId = id
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreatePaymentMethod(IMediator mediator, [FromForm] CreatePaymentMethodCommand command,
        ValidationUtil<CreatePaymentMethodCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdatePaymentMethod(IMediator mediator, [FromRoute] Guid id,
        [FromForm] UpdatePaymentMethodCommand request, ValidationUtil<UpdatePaymentMethodCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }

    #endregion

    #region Brand payment methods

    public async Task<IResult> GetBrandPaymentMethods(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAsc = true,
        [FromQuery] string? code = null,
        [FromQuery] string? name = null,
        [FromQuery] bool? status = null)
    {
        var query = new GetBrandPaymentMethodsQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Name = name,
            Status = status,
            Code = code,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandPublicPaymentMethods(IMediator mediator, [FromRoute] string brandCode)
    {
        var query = new GetPublicBrandPaymentMethodsQuery()
        {
            BrandCode = brandCode,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandPaymentMethodById(IMediator mediator, [FromRoute] Guid id,
        [FromQuery] string timeZone = null)
    {
        var query = new GetBrandPaymentMethodByIdQuery()
        {
            Id = id,
            TimeZone = timeZone,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreateBrandPaymentMethod(IMediator mediator,
        [FromForm] CreateBrandPaymentMethodCommand command,
        ValidationUtil<CreateBrandPaymentMethodCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdateBrandPaymentMethod(IMediator mediator, [FromRoute] Guid id,
        [FromForm] UpdateBrandPaymentMethodCommand request,
        ValidationUtil<UpdateBrandPaymentMethodCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }

    #endregion
}