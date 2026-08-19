using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Authentication;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int ROTATION_THRESHOLD_DAYS = 7;

    public RefreshTokenCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _authenticationService = authenticationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<ApiResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new BadHttpRequestException("HTTP context not available");
        }

        var refreshTokenString = httpContext.Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(refreshTokenString))
        {
            throw new UnauthorizedAccessException("Refresh token not found");
        }

        // BƯỚC 2: Tìm refresh token trong database
        var storedToken = await _unitOfWork.GetRepository<RefreshTokens>().SingleOrDefaultAsync(
            predicate: x => x.Token == refreshTokenString,
            include: x => x.Include(t => t.Account)
                .ThenInclude(a => a.BrandAccounts).ThenInclude(ba => ba.Brand)
        );

        if (storedToken == null)
        {
            _logger.Warning("Refresh token not found in database: {Token}", refreshTokenString);
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // BƯỚC 3: Validate refresh token
        if (storedToken.IsRevoked)
        {
            _logger.Warning(
                "Revoked refresh token used for account {AccountId}",
                storedToken.AccountId);
            throw new UnauthorizedAccessException("Refresh token has been revoked");
        }

        if (storedToken.ExpiryDate <= DateTime.UtcNow)
        {
            _logger.Warning(
                "Expired refresh token used for account {AccountId}",
                storedToken.AccountId);
            throw new UnauthorizedAccessException("Refresh token has expired");
        }

        // BƯỚC 4: Lấy thông tin account
        var account = storedToken.Account;
        if (account == null)
        {
            throw new BadHttpRequestException("Account not found");
        }

        // BƯỚC 5: Generate tokens mới
        var newAccessToken = _authenticationService.GenerateAccessTokenAsync(account);
        var (accessTokenExpiry, refreshTokenExpiryUtc) = _authenticationService.GetJwtExpireConfiguration();

        string newRefreshToken = refreshTokenString;
        DateTime newRefreshTokenExpiryUtc = storedToken.ExpiryDate;
        bool shouldRotateRefreshToken = false;

        // BƯỚC 6: KIỂM TRA CÓ CẦN ROTATE REFRESH TOKEN KHÔNG
        // Chỉ rotate khi refresh token còn < 7 ngày
        var daysUntilExpiry = (storedToken.ExpiryDate - DateTime.UtcNow).TotalDays;
        if (daysUntilExpiry < ROTATION_THRESHOLD_DAYS)
        {
            shouldRotateRefreshToken = true;

            _logger.Information(
                "Refresh token expiring soon ({Days} days left), rotating for account {AccountId}",
                Math.Round(daysUntilExpiry, 1),
                storedToken.AccountId);

            // Generate refresh token mới
            newRefreshToken = _authenticationService.GenerateRefreshTokensAsync(account);
            newRefreshTokenExpiryUtc = refreshTokenExpiryUtc;

            // Revoke token cũ
            storedToken.IsRevoked = true;
            storedToken.RevokedDate = DateTime.UtcNow;
            storedToken.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<RefreshTokens>().UpdateAsync(storedToken);

            var newTokenEntity = new RefreshTokens
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                Token = newRefreshToken,
                ExpiryDate = newRefreshTokenExpiryUtc,
                CreatedDate = DateTime.UtcNow,
                IsRevoked = false
            };

            await _unitOfWork.GetRepository<RefreshTokens>().InsertAsync(newTokenEntity);

            var isSaved = await _unitOfWork.CommitAsync() > 0;
            if (!isSaved)
            {
                _logger.Error("Failed to save new refresh token for account {AccountId}", account.Id);
                throw new Exception("Failed to refresh token");
            }

            _logger.Information("Refresh token rotated successfully for account {AccountId}", account.Id);
        }
        else
        {
            // REFRESH TOKEN VẪN CÒN HẠN LÂU → KHÔNG ROTATE
            // Chỉ trả về access token mới, giữ nguyên refresh token
            _logger.Information(
                "Refresh token still valid ({Days} days remaining), reusing for account {AccountId}",
                Math.Round(daysUntilExpiry, 1),
                storedToken.AccountId);
        }
        
        var brandSetting = SettingUtil.Parse<BrandSetting>(storedToken.Account.BrandAccounts.FirstOrDefault()?.Brand.Configuration);

        if (brandSetting == null)
        {
            throw new BadHttpRequestException("Brand configuration not found");
        }
        
        if ( !brandSetting.EnabledSendEmailFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi email!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.FrontEndUrl))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin cần thiết!");
        }
        var refreshResponse = new LoginResponse()
        {
            Username = account.Username,
            Role = account.Role,
            AccessToken = newAccessToken,
            CookieInfo = new CookieInfo()
            {
                Domain = brandSetting.FrontEndUrl,
                Expiry = TimeUtil.ConvertFromUtc(newRefreshTokenExpiryUtc, request.TimeZone),
                RefreshToken = newRefreshToken,
            }
        };

        return new ApiResponse
        {
            Status = StatusCodes.Status200OK,
            Message = "Token refreshed successfully",
            Data = refreshResponse
        };
    }
}