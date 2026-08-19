using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.SystemConfigs;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Query.GetSystemConfigs;

public class GetSystemConfigsQueryHandler : IRequestHandler<GetSystemConfigsQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IClaimService _claimService;
    private readonly ILogger _logger;

    public GetSystemConfigsQueryHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IClaimService claimService,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _claimService = claimService;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(GetSystemConfigsQuery request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        if (role == null || (role != ERole.SystemAdmin && role != ERole.BrandAdmin))
            return new ApiResponse
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        var configKeys = await _unitOfWork.GetRepository<SystemConfigKeys>().GetListAsync<GetSystemConfigsResponse>(
            orderBy: q => q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.CreatedDate),
            include: q => q
                .Include(x => x.ConfigValues)
                .Include(x => x.DependentDependencies)
                .ThenInclude(d => d.TriggerKey)
        );

        _logger.Information("Lấy danh sách system config thành công: {Count} keys", configKeys.Count);

        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách system config thành công",
            Data = configKeys
        };
    }
}