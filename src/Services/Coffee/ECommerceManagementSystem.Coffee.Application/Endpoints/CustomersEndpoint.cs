using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.CreateCustomerAddress;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.UpdateCustomerAddress;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Query.GetCustomerAddressById;
using ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Query.GetCustomerAddresses;
using ECommerceManagementSystem.Coffee.Application.Features.Customers.Command.SendCustomerConsult;
using ECommerceManagementSystem.Coffee.Application.Features.Customers.query.GetCustomers;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class CustomersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
       var group = app.MapGroup(ApiEndpointConstants.Customers.CustomersEndpoint)
           .WithTags(ApiEndpointConstants.Customers.Tag);
       group.MapGet(ApiEndpointConstants.Customers.GetCustomers, GetCustomers)
           .WithName(nameof(GetCustomers))
           .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
           .Produces<ApiResponse>(StatusCodes.Status200OK)
           .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
       group.MapGet(ApiEndpointConstants.Customers.GetCustomerAddresses, GetCustomerAddresses)
           .WithName(nameof(GetCustomerAddresses))
           .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
           .Produces<ApiResponse>(StatusCodes.Status200OK)
           .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
       group.MapGet(ApiEndpointConstants.Customers.GetCustomerAddressById, GetCustomerAddressById)
           .WithName(nameof(GetCustomerAddressById))
           .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
           .Produces<ApiResponse>(StatusCodes.Status200OK)
           .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
       group.MapPost(ApiEndpointConstants.Customers.CreateCustomerAddress, CreateCustomerAddress)
           .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
           .DisableAntiforgery()
           .WithName(nameof(CreateCustomerAddress))
           .Produces<ApiResponse>(StatusCodes.Status201Created)
           .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
           .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
       group.MapPatch(ApiEndpointConstants.Customers.UpdateCustomerAddress, UpdateCustomerAddress)
           .RequireAuthorization(EPolicy.EndCustomerPolicy.GetDisplayName())
           .DisableAntiforgery()
           .WithName(nameof(UpdateCustomerAddress))
           .Produces<ApiResponse>(StatusCodes.Status202Accepted)
           .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse>(StatusCodes.Status404NotFound)
           .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
       group.MapPost(ApiEndpointConstants.Customers.CreateCustomerConsultant, CreateCustomerConsultant)
           .DisableAntiforgery()
           .WithName(nameof(CreateCustomerConsultant))
           .Produces<ApiResponse>(StatusCodes.Status201Created)
           .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
           .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
           .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
    }

    public async Task<IResult> GetCustomers(IMediator mediator, [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null, [FromQuery] bool isAsc = true, [FromQuery] string? name = null, [FromQuery] EAccountStatus? status = null)
    {
        var query = new GetCustomersQuery()
        {
            Size = size,
            Page = page,
            SortBy = sortBy,
            IsAsc = isAsc,
            Name = name,
            Status = status
        };
        var result = await mediator.Send(query);
        return Results.Ok(result);
    }

    public async Task<IResult> GetCustomerAddresses(IMediator mediator)
    {
        var query = new GetCustomerAddressesQuery(){};
        var result = await mediator.Send(query);
        return Results.Ok(result);
    }
    public async Task<IResult> GetCustomerAddressById(IMediator mediator, [FromRoute] Guid id, [FromQuery] string timeZone)
    {
        var query = new GetCustomerAddressByIdQuery()
        {
            Id = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> UpdateCustomerAddress(IMediator mediator, [FromRoute] Guid id, [FromBody] UpdateCustomerAddressCommand request, ValidationUtil<UpdateCustomerAddressCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }
        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> CreateCustomerAddress(IMediator mediator, [FromBody] CreateCustomerAddressCommand command, ValidationUtil<CreateCustomerAddressCommand> validationUtil)
    {
        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }
        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }
    public async Task<IResult> CreateCustomerConsultant(IMediator mediator, [FromBody] SendCustomerConsultCommand command, ValidationUtil<SendCustomerConsultCommand> validationUtil)
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