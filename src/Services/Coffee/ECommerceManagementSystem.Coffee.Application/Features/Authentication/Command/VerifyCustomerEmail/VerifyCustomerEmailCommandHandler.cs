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
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.VerifyCustomerEmail;

public class VerifyCustomerEmailCommandHandler : IRequestHandler<VerifyCustomerEmailCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VerifyCustomerEmailCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _authenticationService = authenticationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<ApiResponse> Handle(VerifyCustomerEmailCommand request, CancellationToken cancellationToken)
    {
        var existingAccounts = await _unitOfWork.GetRepository<Accounts>().GetListAsync(
            predicate: x => x.Email.Equals(request.Email),
            include: x => x.Include(x => x.CustomerAccounts)
                .ThenInclude(x => x.Customer)
                .ThenInclude(x => x.Brand)
        );
        var existingAccount = existingAccounts
            .FirstOrDefault(x =>
                x.CustomerAccounts.Any(x => x.Customer.Brand.Code.Equals(request.BrandCode)));
        if (existingAccount.IsEmailVerified != null && existingAccount.IsEmailVerified == true)
        {
            throw new BadHttpRequestException("Tài khoản đã được xác thực!");
        }

        if (DateTime.UtcNow > existingAccount.EmailVerificationTokenExpiry)
        {
            throw new BadHttpRequestException("Mã OTP đã hết hạn!");
        }

        if (string.IsNullOrWhiteSpace(existingAccount.EmailVerificationToken) ||
            !existingAccount.EmailVerificationToken.Equals(request.OtpCode))
        {
            throw new BadHttpRequestException("Mã OTP không hợp lệ!");
        }

        existingAccount.Status = EAccountStatus.Active;
        existingAccount.IsEmailVerified = true;
        existingAccount.EmailVerifiedDate = DateTime.UtcNow;
        existingAccount.EmailVerificationToken = null;
        existingAccount.EmailVerificationTokenExpiry = null;
        existingAccount.LastOtpSentAt = null;

        var accessTokenString = _authenticationService.GenerateAccessTokenAsync(existingAccount);
        var refreshTokenString = _authenticationService.GenerateRefreshTokensAsync(existingAccount);
        var (accessTokenExpiryFromUtc, refreshTokenExpiryUtc) = _authenticationService.GetJwtExpireConfiguration();
        var refreshTokenExpiry = TimeUtil.ConvertFromUtc(refreshTokenExpiryUtc, request.TimeZone);
        var refreshToken = new RefreshTokens()
        {
            Id = Guid.CreateVersion7(),
            AccountId = existingAccount.Id,
            Token = refreshTokenString,
            ExpiryDate = refreshTokenExpiry,
            CreatedDate = DateTime.UtcNow,
            IsRevoked = false
        };
        var beginResult = await _unitOfWork.BeginTransactionAsync();
        if (!beginResult.IsSuccess)
        {
            _logger.Error($"Failed to begin transaction: {beginResult.Message}");
            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = $"Failed to begin transaction: {beginResult.Message}",
            };
        }

        _unitOfWork.GetRepository<Accounts>().UpdateAsync(existingAccount);
        await _unitOfWork.GetRepository<RefreshTokens>().InsertAsync(refreshToken);

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            if (commitResult.ValidationErrors?.Any() == true)
            {
                foreach (var error in commitResult.ValidationErrors)
                {
                    _logger.Warning(
                        $"Validation Error - {string.Join(", ", error.MemberNames)}: {error.ErrorMessage}");
                }
            }
            else
            {
                _logger.Error($"Transaction failed: {commitResult.Message}", commitResult.Exception);
            }

            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể tạo thương hiệu!");
        }

        if (commitResult.RowsAffected < 2)
        {
            _logger.Warning($"Expected 2 rows but only {commitResult.RowsAffected} were affected");
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể đang nhập!");
        }

        var brandSetting = SettingUtil.Parse<BrandSetting>(existingAccounts
            .FirstOrDefault(x =>
                x.CustomerAccounts.Any(x => x.Customer.Brand.Code.Equals(request.BrandCode))).CustomerAccounts
            .FirstOrDefault(x => x.Customer.Brand.Code.Equals(request.BrandCode)).Customer.Brand.Configuration);

        if (brandSetting == null)
        {
            throw new BadHttpRequestException("Brand configuration not found");
        }

        if (!brandSetting.EnabledSendEmailFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi email!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.FrontEndUrl))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin cần thiết!");
        }

        var loginResponse = new LoginResponse()
        {
            Username = existingAccount.Username,
            Role = existingAccount.Role,
            AccessToken = accessTokenString,
            CookieInfo = new CookieInfo()
            {
                Domain = brandSetting.FrontEndUrl,
                Expiry = refreshTokenExpiry,
                RefreshToken = refreshTokenString,
            }
        };
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Xác thực tài khoản thành công!",
            Data = loginResponse
        };
    }
}