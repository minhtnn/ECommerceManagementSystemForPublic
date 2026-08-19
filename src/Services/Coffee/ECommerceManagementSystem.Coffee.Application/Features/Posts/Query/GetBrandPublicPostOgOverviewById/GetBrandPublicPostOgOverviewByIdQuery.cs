using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPostOgOverviewById;

public class GetBrandPublicPostOgOverviewByIdQuery : IRequest<IActionResult>
{
    public required string BrandCode { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public required string TimeZone { get; set; }
}