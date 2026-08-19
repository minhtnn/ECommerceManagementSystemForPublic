using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Authentication;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using FirebaseAdmin.Auth;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerGoogleLoginAndRegister;

public class CustomerGoogleLoginAndRegisterCommandHandler
    : IRequestHandler<CustomerGoogleLoginAndRegisterCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly ILogger _logger;
    private readonly IAuthenticationService _authenticationService;

    public CustomerGoogleLoginAndRegisterCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation,
        ILogger logger,
        IAuthenticationService authenticationService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _logger = logger;
        _authenticationService = authenticationService;
    }

    public async ValueTask<ApiResponse> Handle(
        CustomerGoogleLoginAndRegisterCommand request,
        CancellationToken cancellationToken)
    {
        #region Verify Firebase Token

        FirebaseToken decodedToken;
        try
        {
            decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(request.IdToken, cancellationToken);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.Warning("Invalid Firebase token: {Error}", ex.Message);
            throw new BadHttpRequestException("Google token không hợp lệ hoặc đã hết hạn!");
        }

        var googleId = decodedToken.Uid;
        var email = decodedToken.Claims.GetValueOrDefault("email")?.ToString();
        var emailVerified = decodedToken.Claims.GetValueOrDefault("email_verified") as bool? ?? false;
        var googleName = decodedToken.Claims.GetValueOrDefault("name")?.ToString();
        var googlePicture = decodedToken.Claims.GetValueOrDefault("picture")?.ToString();

        if (string.IsNullOrEmpty(email))
        {
            throw new BadHttpRequestException("Không thể lấy email từ tài khoản Google!");
        }

        #endregion

        #region Validate Brand

        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.BrandCode)
            );

        if (existedBrand == null)
        {
            throw new BadHttpRequestException("Thương hiệu không tồn tại!");
        }

        if (existedBrand.Status == EBrandStatus.Inactive)
        {
            throw new BadHttpRequestException("Thương hiệu bị tạm dừng! Xin liên hệ tới quản trị viên!");
        }

        #endregion
        
        var brandSetting = SettingUtil.Parse<BrandSetting>(existedBrand.Configuration);

        if (!brandSetting.EnabledSendEmailFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi email!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.FrontEndUrl))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin cần thiết!");
        }

        #region Find Existing Account (scoped to Brand + EndCustomer)

        var existingAccount = await _unitOfWork.GetRepository<Accounts>()
            .SingleOrDefaultAsync(
                predicate: x =>
                    (x.Email == email || x.GoogleId == googleId) &&
                    x.Role == ERole.EndCustomer &&
                    x.CustomerAccounts.Any(ca => ca.Customer.BrandId == existedBrand.Id),
                include: x => x.Include(a => a.CustomerAccounts)
                    .ThenInclude(ca => ca.Customer)
            );

        #endregion

        // ── Phân nhánh LOGIN / REGISTER ──────────────────────────────────────
        Accounts account;
        Domain.Entities.Customers? newCustomer = null;
        bool isNewAccount = existingAccount == null;

        if (!isNewAccount)
        {
            // ════════════════════════════════════════════════════
            // 4a. LOGIN FLOW
            // ════════════════════════════════════════════════════
            account = existingAccount!;

            #region Check Account Status

            if (account.Status == EAccountStatus.Inactive)
            {
                return new ApiResponse()
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Tài khoản đã bị tạm khóa!",
                };
            }

            #endregion

            #region Sync GoogleId (nếu user đã đăng ký email thường trước đó)

            if (string.IsNullOrEmpty(account.GoogleId))
            {
                account.GoogleId = googleId;
                account.AuthProvider = EAuthProvider.Google;
                _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);
            }

            #endregion

            _logger.Information(
                "Google login successful — AccountId: {AccountId}, Email: {Email}, Brand: {BrandCode}",
                account.Id, email, request.BrandCode
            );
        }
        else
        {
            // ════════════════════════════════════════════════════
            // 4b. REGISTER FLOW
            // ════════════════════════════════════════════════════

            #region Block nếu email/googleId thuộc BrandAdmin

            var isBrandAdmin = await _unitOfWork.GetRepository<Accounts>()
                .SingleOrDefaultAsync(
                    predicate: x =>
                        (x.Email == email || x.GoogleId == googleId) &&
                        x.Role == ERole.BrandAdmin
                ) != null;

            if (isBrandAdmin)
            {
                return new ApiResponse()
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Email này không hợp lệ. Vui lòng sử dụng email khác!"
                };
            }

            #endregion

            #region Start Transaction

            var transactionResult = await _unitOfWork.BeginTransactionAsync();
            if (!transactionResult.IsSuccess)
            {
                _logger.Error("Không thể bắt đầu transaction: {Message}", transactionResult.Message);
                return new ApiResponse()
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = "Không thể bắt đầu transaction"
                };
            }

            #endregion

            #region Create Account + Customer + CustomerAccount

            account = new Accounts()
            {
                Id = Guid.CreateVersion7(),
                Role = ERole.EndCustomer,
                Email = email,
                Username = GenerateUsernameFromEmail(email),
                PhoneNumber = null,
                IsPhoneVerified = false,
                IsEmailVerified = emailVerified,
                EmailVerifiedDate = emailVerified ? DateTime.UtcNow : null,
                Status = EAccountStatus.Active,
                GoogleId = googleId,
                AuthProvider = EAuthProvider.Google,
                PasswordHash = null,
                PasswordSalt = null,
                CreatedDate = DateTime.UtcNow,
            };

            newCustomer = new Domain.Entities.Customers()
            {
                Id = Guid.CreateVersion7(),
                BrandId = existedBrand.Id,
                Email = email,
                FullName = googleName ?? email.Split('@')[0],
                PhoneNumber = null,
                AvatarUrl = googlePicture,
                CreatedDate = DateTime.UtcNow,
            };

            var customerAccount = new CustomerAccounts()
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                CustomerId = newCustomer.Id,
                CreatedDate = DateTime.UtcNow,
                Account = account,
                Customer = newCustomer,
            };

            await _unitOfWork.GetRepository<CustomerAccounts>().InsertAsync(customerAccount);

            var commitResult = await _unitOfWork.CommitTransactionAsync();
            if (!commitResult.IsSuccess)
            {
                _logger.Error(
                    "Transaction commit failed: {Message}. Exception: {Exception}",
                    commitResult.Message,
                    commitResult.Exception?.Message
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Không thể tạo tài khoản: {commitResult.Message}");
            }

            #endregion

            _logger.Information(
                "Google register successful — AccountId: {AccountId}, CustomerId: {CustomerId}, Email: {Email}, Brand: {BrandCode}",
                account.Id, newCustomer.Id, email, request.BrandCode
            );
        }

        #region Generate JWT Tokens (dùng chung cho cả 2 nhánh)

        var accessTokenString = _authenticationService.GenerateAccessTokenAsync(account);
        var refreshTokenString = _authenticationService.GenerateRefreshTokensAsync(account);
        var (_, refreshTokenExpiryUtc) = _authenticationService.GetJwtExpireConfiguration();
        var refreshToken = new RefreshTokens()
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            Token = refreshTokenString,
            ExpiryDate = refreshTokenExpiryUtc,
            CreatedDate = DateTime.UtcNow,
            IsRevoked = false
        };

        await _unitOfWork.GetRepository<RefreshTokens>().InsertAsync(refreshToken);
        var isSaveRefreshToken = await _unitOfWork.CommitAsync() > 0;

        if (!isSaveRefreshToken)
        {
            _logger.Warning("Cannot save refresh token for AccountId: {AccountId}", account.Id);
        }

        #endregion

        #region Invalidate Cache (chỉ khi Register)

        if (isNewAccount && newCustomer != null)
        {
            var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
                lockKey: CacheConfig.EntityInvalidationLock(
                    CacheConfig.EntityDetailCachePrefix(nameof(Customers), newCustomer.Id.ToString())),
                operation: EOperationBeforeCache.BulkCreate,
                counterKey: CacheConfig.EntityInvalidationCounter(
                    CacheConfig.EntityDetailCachePrefix(nameof(Customers), newCustomer.Id.ToString())),
                entityCachePrefix: CacheConfig.EntityDetailCachePrefix(
                    nameof(Customers), newCustomer.Id.ToString())
            );

            if (!cacheResult.Success)
            {
                _logger.Warning(
                    "Cache invalidation failed after register: {Message}", cacheResult.Message);
            }
        }

        #endregion
        var refreshTokenExpiry = TimeUtil.ConvertFromUtc(refreshTokenExpiryUtc, request.TimeZone);
        var loginResponse = new LoginResponse()
        {
            Username = account.Username,
            Role = account.Role,
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
            Status = isNewAccount ? StatusCodes.Status201Created : StatusCodes.Status200OK,
            Message = isNewAccount ? "Đăng ký và đăng nhập thành công!" : "Đăng nhập thành công!",
            Data = loginResponse
        };
    }

    #region Helper Methods

    private string GenerateUsernameFromEmail(string email)
    {
        var emailPrefix = email.Split('@')[0];
        var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        return $"{emailPrefix}_{randomSuffix}".ToLower();
    }

    #endregion
}