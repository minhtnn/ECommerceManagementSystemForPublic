using System.Security.Authentication;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Authentication;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Query.GetAccountDetail;

public class GetAccountQueryHandler : IRequestHandler<GetAccountDetailQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;
    private readonly IMediaService _mediaService;
    
    public GetAccountQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        IClaimService claimService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(GetAccountDetailQuery request, CancellationToken cancellationToken)
    {
        var accountId = _claimService.GetCurrentAccountId();
        var role = _claimService.GetCurrentRoleEnum();
        var accountDetailResponse = new AccountDetailResponse();
        if (accountId == null || role == null || !Enum.IsDefined(typeof(ERole), role))
        {
            throw new AuthenticationException("Tài khoản không tồn tại!");
        }

        var account = await _unitOfWork.GetRepository<Accounts>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == accountId
            );
        if (account == null)
        {
            throw new BadHttpRequestException("Không tìm thấy tài khoản này!");
        }

        if (account.Status == EAccountStatus.Lock)
        {
            throw new BadHttpRequestException("Tài khoản đã bị khóa! Xin vui lòng liên hệ quản trị viên!");
        }

        accountDetailResponse.Username = account.Username ?? String.Empty;
        accountDetailResponse.Email = account.Email;
        accountDetailResponse.PhoneNumber = account.PhoneNumber ?? String.Empty;

        switch (role)
        {
            case ERole.SystemAdmin:

                break;
            case ERole.BrandAdmin:
                var brandId = _claimService.GetCurrentReferenceId();
                if (brandId == null)
                {
                    throw new AuthenticationException("Tài khoản không tồn tại!");
                }

                var brand = await _unitOfWork.GetRepository<Domain.Entities.Brands>().SingleOrDefaultAsync(
                    predicate: x => x.Id == brandId
                );
                if (brand.Status == EBrandStatus.Inactive)
                {
                    throw new BadHttpRequestException("Thương hiệu bị tạm dừng! Xin liên hệ tới quản trị viên!");
                }
                accountDetailResponse.Name = brand.Name;
                accountDetailResponse.FullName = brand.Fullname;
                accountDetailResponse.Address = brand.Address ?? String.Empty;
                if (brand.LogoUrl != null && !string.IsNullOrEmpty(brand.LogoUrl))
                {
                    try
                    {
                        accountDetailResponse.ImageUrl = await _mediaService.GetImageUrlAsync(
                            brand.LogoUrl,
                            TimeSpan.FromHours(1)
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(
                            "Failed to generate signed URL for image {ImageUrl}: {Error}",
                            brand.LogoUrl,
                            ex.Message
                        );
                    }
                }
                break;
            case ERole.EndCustomer:
                var customerId = _claimService.GetCurrentReferenceId();
                if (customerId == null)
                {
                    throw new AuthenticationException("Tài khoản không tồn tại!");
                }

                var customer = await _unitOfWork.GetRepository<Domain.Entities.Customers>().SingleOrDefaultAsync(
                    predicate: x => x.Id == customerId
                );
                accountDetailResponse.FullName = customer.FullName;
                accountDetailResponse.Address = String.Empty;
                if (customer.AvatarUrl != null && !string.IsNullOrEmpty(customer.AvatarUrl))
                {
                    try
                    {
                        accountDetailResponse.ImageUrl = await _mediaService.GetImageUrlAsync(
                            customer.AvatarUrl,
                            TimeSpan.FromHours(1)
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(
                            "Failed to generate signed URL for image {ImageUrl}: {Error}",
                            customer.AvatarUrl,
                            ex.Message
                        );
                    }
                }
                break;
            default:
                break;
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Truy cập thành công!",
            Data = accountDetailResponse
        };
    }
}