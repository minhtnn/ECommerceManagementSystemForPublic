using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.CreateOrder;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.UpdateOrder;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetBrandOrders;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetCustomerOrders;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetOrderById;
using ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetPaymentLink;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class OrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Order.OrdersEndpoint)
            .WithTags(ApiEndpointConstants.Order.Tag);
        group.MapGet(ApiEndpointConstants.Order.GetBrandOrders, GetBrandOrders)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetBrandOrders))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Order.GetCustomerOrders, GetCustomerOrders)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(GetCustomerOrders))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Order.GetOrderById, GetOrderById)
            .RequireAuthorization(EPolicy.BrandOrEndCustomerPolicy.GetDisplayName())
            .WithName(nameof(GetOrderById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Order.CreateOrder, CreateOrder)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(CreateOrder))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.Order.UpdateOrder, UpdateOrder)
            .RequireAuthorization(EPolicy.BrandOrEndCustomerPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(UpdateOrder))
            .Produces<ApiResponse>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Order.GetPaymentLink, GetPaymentLink)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(GetPaymentLink))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest);
    }

    public async Task<IResult> GetBrandOrders(IMediator mediator, [FromQuery] int page = 1, [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, [FromQuery] bool isAsc = true, [FromQuery] string? searchKeyword = null,
        [FromQuery] EOrderStatus? orderStatus = null, [FromQuery] EPaymentStatus? paymentStatus = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? timeZone = null)
    {
        var query = new GetBrandOrdersQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            SearchKeyword = searchKeyword,
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus,
            FromDate = fromDate,
            ToDate = toDate,
            TimeZone = timeZone,
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public static async Task<IResult> GetPaymentLink(
        IMediator mediator,
        [FromRoute] Guid id)
    {
        var query = new GetPaymentLinkQuery()
        {
            OrderId = id
        };

        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse, statusCode: apiResponse.Status);
    }

    public async Task<IResult> GetCustomerOrders(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? searchKeyword = null,
        [FromQuery] EOrderStatus? orderStatus = null,
        [FromQuery] EPaymentStatus? paymentStatus = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAsc = true,
        [FromQuery] string timeZone = null)
    {
        var query = new GetCustomerOrdersQuery()
        {
            Page = page,
            Size = size,
            SearchKeyword = searchKeyword,
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus,
            SortBy = sortBy,
            IsAsc = isAsc,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetOrderById(IMediator mediator, [FromRoute] Guid id, [FromQuery] string timeZone = null)
    {
        var query = new GetOrderByIdQuery()
        {
            OrderId = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreateOrder(IMediator mediator, [FromBody] CreateOrderCommand command,
        ValidationUtil<CreateOrderCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdateOrder(IMediator mediator, [FromRoute] Guid id,
        [FromBody] UpdateOrderCommand request, ValidationUtil<UpdateOrderCommand> validationUtil)
    {
        request.OrderId = id;
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }
}