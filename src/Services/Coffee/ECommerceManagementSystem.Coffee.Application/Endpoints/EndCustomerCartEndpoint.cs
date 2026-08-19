using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.CreateCart;
using ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.UpdateCart;
using ECommerceManagementSystem.Coffee.Application.Features.Carts.Query.GetCustomerCart;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class EndCustomerCartEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Cart.EndCustomerCartsEndpoint)
            .WithTags(ApiEndpointConstants.Cart.Tag);

        group.MapGet(ApiEndpointConstants.Cart.GetCustomerCart, GetCustomerCart)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(GetCustomerCart))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

        group.MapPost(ApiEndpointConstants.Cart.CreateCart, CreateCart)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(CreateCart))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);

        group.MapPut(ApiEndpointConstants.Cart.UpdateCart, UpdateCart)
            .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
            .WithName(nameof(UpdateCart))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Lấy giỏ hàng hiện tại của customer
    /// </summary>
    public async Task<IResult> GetCustomerCart(IMediator mediator)
    {
        var query = new GetCustomerCartCommand();
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    /// <summary>
    /// Tạo giỏ hàng mới (hoặc lấy cart hiện có nếu đã tồn tại)
    /// </summary>
    public async Task<IResult> CreateCart(
        IMediator mediator,
        [FromBody] CreateCartCommand command,
        ValidationUtil<CreateCartCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        
        // Nếu cart đã tồn tại → 200 OK
        // Nếu tạo mới thành công → 201 Created
        if (apiResponse.Status == StatusCodes.Status201Created)
        {
            return Results.Created($"{ApiEndpointConstants.Cart.EndCustomerCartsEndpoint}", apiResponse);
        }
        
        return Results.Json(apiResponse);
    }

    /// <summary>
    /// Update giỏ hàng (thêm/xóa/sửa sản phẩm, cập nhật note, promotions)
    /// Tự động tạo cart nếu chưa có
    /// </summary>
    public async Task<IResult> UpdateCart(
        IMediator mediator,
        [FromBody] UpdateCartCommand command,
        ValidationUtil<UpdateCartCommand> validationUtil)
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