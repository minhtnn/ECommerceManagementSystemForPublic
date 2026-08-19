using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Authentication;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IAuthenticationService _authenticationService;

    public LoginCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        IAuthenticationService authenticationService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _authenticationService = authenticationService;
    }

    public async ValueTask<ApiResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.BrandCode)
            );

        if (existedBrand == null)
        {
            throw new BadHttpRequestException("Thương hiệu không tồn tại!");
        }

        var brandSetting = SettingUtil.Parse<BrandSetting>(existedBrand.Configuration);

        if (!brandSetting.EnabledSendEmailFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi email!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.FrontEndUrl))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin cần thiết!");
        }

        var loginIdentifier = request.Email ?? request.Username;
        var existedAccount = await _unitOfWork.GetRepository<Accounts>().SingleOrDefaultAsync(
            include: x => x.Include(x => x.BrandAccounts)
                .Include(x => x.CustomerAccounts)
                .ThenInclude(x => x.Customer)
                .ThenInclude(x => x.Brand),
            predicate: x =>
                (x.Email.Equals(loginIdentifier) ||
                 (!string.IsNullOrWhiteSpace(x.Username) && x.Username.Equals(loginIdentifier)))
                && ((x.CustomerAccounts.Any(y => y.Customer.Brand.Code.Equals(request.BrandCode)))
                    || (x.Role == ERole.BrandAdmin &&
                        x.BrandAccounts.Any(ba => ba.Brand.Code.Equals(request.BrandCode)))
                    || (x.Role == ERole.SystemAdmin))
        );
        if (existedAccount == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Không tìm thấy tài khoản",
            };
        }
        
        if (existedAccount.Status == EAccountStatus.Lock)
        {
            throw new BadHttpRequestException("Tài khoản đã bị khóa! Xin vui lòng liên hệ quản trị viên!");
        }
        if (existedBrand.Status == EBrandStatus.Inactive && existedAccount.Role != ERole.SystemAdmin)
        {
            throw new BadHttpRequestException("Thương hiệu bị tạm dừng! Xin liên hệ tới quản trị viên!");
        }

        var isValidPassword =
            AuthenUtil.Verify(request.Password, existedAccount.PasswordHash!, existedAccount.PasswordSalt!);
        if (!isValidPassword)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status404NotFound,
                Message = "Sai tài khoản hoặc mật khẩu",
            };
        }

        var accessTokenString = _authenticationService.GenerateAccessTokenAsync(existedAccount);
        var refreshTokenString = _authenticationService.GenerateRefreshTokensAsync(existedAccount);
        var (accessTokenExpiryUtc, refreshTokenExpiryUtc) = _authenticationService.GetJwtExpireConfiguration();
        var refreshToken = new RefreshTokens()
        {
            Id = Guid.CreateVersion7(),
            AccountId = existedAccount.Id,
            Token = refreshTokenString,
            ExpiryDate = refreshTokenExpiryUtc,
            CreatedDate = DateTime.UtcNow,
            IsRevoked = false
        };
        await _unitOfWork.GetRepository<RefreshTokens>().InsertAsync(refreshToken);
        var isSaveRefreshToken = await _unitOfWork.CommitAsync() > 0;
        if (!isSaveRefreshToken)
        {
            _logger.Warning("Cannot save refresh token: {token}", refreshTokenString);
        }
        
        var loginResponse = new LoginResponse()
        {
            Username = existedAccount.Username,
            Role = existedAccount.Role,
            AccessToken = accessTokenString,
            CookieInfo = new CookieInfo()
            {
                Domain = brandSetting.FrontEndUrl,
                Expiry = TimeUtil.ConvertFromUtc(refreshTokenExpiryUtc, request.TimeZone),
                RefreshToken = refreshTokenString,
            }
        };
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Đăng nhập thành công!",
            Data = loginResponse
        };
    }
}