using Carter;
using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.CreateBrandPost;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.UpdateBrandPost;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPostById;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPosts;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPostById;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPostOgOverviewById;
using ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPosts;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Extensions;

namespace ECommerceManagementSystem.Coffee.Application.Endpoints;

public class PostEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiEndpointConstants.Post.PostsEndpoint)
            .WithTags(ApiEndpointConstants.Post.Tag);
        group.MapGet(ApiEndpointConstants.Post.GetBrandPosts, GetBrandPosts)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetBrandPosts))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Post.GetBrandPostById, GetBrandPostById)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .WithName(nameof(GetBrandPostById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPost(ApiEndpointConstants.Post.CreateBrandPost, CreateBrandPost)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(CreateBrandPost))
            .Produces<ApiResponse>(StatusCodes.Status201Created)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapPatch(ApiEndpointConstants.Post.UpdateBrandPost, UpdateBrandPost)
            .RequireAuthorization(EPolicy.BrandPolicy.GetDisplayName())
            .DisableAntiforgery()
            .WithName(nameof(UpdateBrandPost))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Post.GetBrandPublicPosts, GetBrandPublicPosts)
            .WithName(nameof(GetBrandPublicPosts))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Post.GetBrandPublicPostById, GetBrandPublicPostById)
            .WithName(nameof(GetBrandPublicPostById))
            .Produces<ApiResponse>(StatusCodes.Status200OK)
            .Produces<ApiResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse>(StatusCodes.Status500InternalServerError);
        group.MapGet(ApiEndpointConstants.Post.GetBrandPublicPostOgPreviewById, GetBrandPublicPostOgPreviewById)
            .WithName(nameof(GetBrandPublicPostOgPreviewById))
            .AllowAnonymous()
            .RequireCors(CorsPolicy.AllowPublic)
            .Produces<IActionResult>(StatusCodes.Status200OK)
            .Produces<IActionResult>(StatusCodes.Status404NotFound)
            .Produces<IActionResult>(StatusCodes.Status500InternalServerError);
        group.MapMethods(ApiEndpointConstants.Post.GetBrandPublicPostOgPreviewById,
                new[] { "HEAD" }, GetBrandPublicPostOgPreviewById)
            .AllowAnonymous()
            .RequireCors(CorsPolicy.AllowPublic);
    }

    public async Task<IResult> GetBrandPosts(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAsc = true,
        [FromQuery] string? code = null,
        [FromQuery] EPostStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? timeZone = null)
    {
        var query = new GetBrandPostsQuery()
        {
            Page = page,
            Size = size,
            SortBy = sortBy,
            IsAsc = isAsc,
            Code = code,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandPostById(
        IMediator mediator,
        [FromRoute] Guid id,
        [FromQuery] string timeZone)
    {
        var query = new GetBrandPostByIdQuery() { Id = id, TimeZone = timeZone };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> CreateBrandPost(
        IMediator mediator,
        HttpContext httpContext,
        [FromForm] CreateBrandPostCommand command,
        ValidationUtil<CreateBrandPostCommand> validationUtil)
    {
        var inlineFiles = httpContext.Request.Form.Files
            .Where(f => f.Name.StartsWith(
                "InlineImages", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name)
            .ToList();

        command.InlineImages = inlineFiles.Count > 0
            ? new FormFileCollectionWrapperUtil(inlineFiles)
            : null;

        var (isValid, response) = await validationUtil.ValidateAsync(command);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(command);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> UpdateBrandPost(
        IMediator mediator,
        HttpContext httpContext,
        [FromRoute] Guid id,
        [FromForm] UpdateBrandPostCommand request,
        ValidationUtil<UpdateBrandPostCommand> validationUtil)
    {
        request.Id = id;
        var inlineFiles = httpContext.Request.Form.Files
            .Where(f => f.Name.StartsWith(
                "InlineImages", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name)
            .ToList();

        request.InlineImages = inlineFiles.Count > 0
            ? new FormFileCollectionWrapperUtil(inlineFiles)
            : null;
        var (isValid, response) = await validationUtil.ValidateAsync(request);
        if (!isValid)
        {
            return Results.BadRequest(response);
        }

        var apiResponse = await mediator.Send(request);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandPublicPosts(
        IMediator mediator,
        [FromRoute] string brandCode,
        [FromQuery] int page = 1,
        [FromQuery] int size = 30,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isAsc = true,
        [FromQuery] string timeZone = null
    )
    {
        var query = new GetBrandPublicPostsQuery()
        {
            BrandCode = brandCode,
            Page = page,
            Size = size,
            SortBy = sortBy,
            IsAsc = isAsc,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandPublicPostById(
        IMediator mediator,
        [FromRoute] string brandCode,
        [FromRoute] Guid id,
        [FromQuery] string timeZone = null)
    {
        var query = new GetBrandPublicPostByIdQuery()
        {
            BrandCode = brandCode,
            Id = id,
            TimeZone = timeZone
        };
        var apiResponse = await mediator.Send(query);
        return Results.Json(apiResponse);
    }

    public async Task<IResult> GetBrandPublicPostOgPreviewById(
        IMediator mediator,
        [FromRoute] string brandCode,
        [FromRoute] Guid id,
        [FromQuery] string? timeZone = null)
    {
        var query = new GetBrandPublicPostOgOverviewByIdQuery
        {
            BrandCode = brandCode,
            Id = id,
            TimeZone = timeZone ?? "SE Asia Standard Time"
        };

        var result = await mediator.Send(query);

        // Chuyển IActionResult → IResult để Carter hiểu
        return result switch
        {
            ContentResult cr => Results.Content(cr.Content!, cr.ContentType!, statusCode: cr.StatusCode),
            NotFoundResult => Results.NotFound(),
            _ => Results.StatusCode(500)
        };
    }
}