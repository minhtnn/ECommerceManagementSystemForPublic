using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;

    private readonly ILogger _logger;

    // private readonly IConfiguration _configuration;
    private readonly IMediaService _mediaService;

    public ForgotPasswordCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        ILogger logger,
        IConfiguration configuration, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _logger = logger;
        // _configuration = configuration;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        #region 1. Validate Brand

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

        var brandSetting = SettingUtil.Parse<BrandSetting>(existedBrand.Configuration);

        if (!brandSetting.EnabledSendEmailFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi email!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.SendGridApiKey) ||
            string.IsNullOrWhiteSpace(brandSetting.SendGridFromEmail) ||
            string.IsNullOrWhiteSpace(brandSetting.SendGridFromName))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin gửi email!");
        }

        if (!brandSetting.EnabledForgotPasswordFunction)
        {
            throw new BadHttpRequestException("Thương hiệu chưa bật chức năng gửi quên mật khẩu!");
        }

        if (string.IsNullOrWhiteSpace(brandSetting.FrontEndUrl) ||
            string.IsNullOrWhiteSpace(brandSetting.FrontEndAuthPath))
        {
            throw new BadHttpRequestException("Thương hiệu chưa cấu hình đầy đủ thông tin chức năng!");
        }

        #endregion

        #region 2. Find Account

        var account = await _unitOfWork.GetRepository<Accounts>()
            .SingleOrDefaultAsync(
                predicate: x =>
                    x.Email == request.Email &&
                    (
                        (x.Role == ERole.EndCustomer &&
                         x.CustomerAccounts.Any(ca => ca.Customer.BrandId == existedBrand.Id)) ||
                        (x.Role == ERole.BrandAdmin && x.BrandAccounts.Any(ba => ba.BrandId == existedBrand.Id)) ||
                        (x.Role == ERole.SystemAdmin)
                    ),
                include: x => x.Include(a => a.CustomerAccounts)
                    .ThenInclude(ca => ca.Customer)
                    .Include(a => a.BrandAccounts)
            );

        if (account == null)
        {
            _logger.Warning(
                "Password reset requested for non-existent email: {Email}, Brand: {BrandCode}",
                request.Email,
                request.BrandCode
            );

            // Return fake success to prevent email enumeration
            return new ApiResponse()
            {
                Status = StatusCodes.Status200OK,
                Message = "Nếu email tồn tại, link đặt lại mật khẩu đã được gửi đến email của bạn."
            };
        }

        #endregion

        #region 3. Check Auth Provider

        if (account.AuthProvider == EAuthProvider.Google)
        {
            throw new BadHttpRequestException(
                "Tài khoản này đăng nhập bằng Google. Vui lòng sử dụng 'Đăng nhập với Google'."
            );
        }

        #endregion

        #region 4. Check Account Status

        if (account.Status == EAccountStatus.Inactive)
        {
            throw new BadHttpRequestException("Tài khoản đã bị khóa!");
        }

        if (account.Role == ERole.BrandAdmin)
        {
            throw new BadHttpRequestException("Chức năng không hỗ trợ! Vui lòng liên hệ quản trị viên!");
        }

        #endregion

        #region 5. Check Rate Limiting

        if (account.LastOtpSentAt.HasValue)
        {
            var timeSinceLastOtp = DateTime.UtcNow - account.LastOtpSentAt.Value;
            var cooldownSeconds = 60; // 1 minute cooldown

            if (timeSinceLastOtp.TotalSeconds < cooldownSeconds)
            {
                var remainingSeconds = (int)(cooldownSeconds - timeSinceLastOtp.TotalSeconds);
                throw new BadHttpRequestException(
                    $"Vui lòng đợi {remainingSeconds} giây trước khi yêu cầu lại!"
                );
            }
        }

        #endregion

        #region 6. Check Account Lock (Anti-brute force)

        if (account.PasswordResetLockedUntil.HasValue &&
            account.PasswordResetLockedUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(account.PasswordResetLockedUntil.Value - DateTime.UtcNow).TotalMinutes;
            throw new BadHttpRequestException(
                $"Tài khoản tạm thời bị khóa do quá nhiều lần thử. Vui lòng thử lại sau {remainingMinutes} phút."
            );
        }

        #endregion

        #region 7. Generate Token

        var resetToken = AuthenUtil.GeneratePasswordResetToken();
        var tokenExpiry = AuthenUtil.GeneratePasswordResetTokenExpiry(minutesToExpire: 15);

        #endregion

        #region 8. Update Account with Token (Transaction)

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Failed to begin transaction: {Message}", transactionResult.Message);
            throw new Exception("Không thể bắt đầu transaction");
        }

        try
        {
            // Update account
            account.PasswordResetToken = resetToken;
            account.PasswordResetTokenExpiry = tokenExpiry;
            account.PasswordResetTokenUsed = false;
            account.PasswordResetTokenUsedAt = null;
            account.LastOtpSentAt = DateTime.UtcNow;
            account.LastModifiedDate = DateTime.UtcNow;

            _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);

            var commitResult = await _unitOfWork.CommitTransactionAsync();

            if (!commitResult.IsSuccess)
            {
                _logger.Error(
                    "Transaction commit failed: {Message}",
                    commitResult.Message
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Không thể cập nhật token");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating account with reset token");
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        #endregion

        #region 9. Build Reset URL

        var resetUrl =
            $"https://{brandSetting.FrontEndUrl}/{brandSetting.FrontEndAuthPath}#token={resetToken}&email={Uri.EscapeDataString(account.Email)}";

        #endregion

        #region 10. Send Email

        try
        {
            string logoBase64String = null;
            if (!string.IsNullOrWhiteSpace(existedBrand.LogoUrl))
            {
                try
                {
                    logoBase64String = await _mediaService.GetImageUrlAsync(
                        existedBrand.LogoUrl,
                        TimeSpan.FromHours(1)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to get image url", ex);
                }
            }

            var sendPasswordResetLinkEmailRequest = new SendPasswordResetLinkEmailRequest
            {
                BrandLogoBase64 = logoBase64String,
                BrandName = existedBrand.Name,
                CustomerName = account.Username ?? "Khách hàng",
                ToEmail = account.Email,
                ResetUrl = resetUrl,
                ExpiryTime = tokenExpiry,
                TimeMeasureUnit = "phút"
            };
            var emailResult = await _emailService.SendPasswordResetLinkAsync(
                brandSetting.SendGridApiKey,
                brandSetting.SendGridFromEmail,
                brandSetting.SendGridFromName,
                mainColor: brandSetting.MainColor,
                sendPasswordResetLinkEmailRequest,
                cancellationToken: cancellationToken
            );

            if (!emailResult.IsSuccess)
            {
                _logger.Warning(
                    "Failed to send password reset email to {Email}: {Error}",
                    account.Email,
                    emailResult.Message
                );
            }
        }
        catch (Exception emailEx)
        {
            _logger.Error(emailEx, "Exception sending password reset email");
        }

        #endregion

        #region 11. Log Audit Event

        await LogAuditEventAsync(
            accountId: account.Id,
            action: PasswordResetAuditActions.TokenRequested,
            success: true,
            partialToken: AuthenUtil.GetPartialToken(resetToken)
        );

        #endregion

        _logger.Information(
            "Password reset requested for Account: {AccountId}, Email: {Email}, Brand: {BrandCode}",
            account.Id,
            account.Email,
            request.BrandCode
        );

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Link đặt lại mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.",
            Data = new
            {
                Email = account.Email,
                ExpiryTime = tokenExpiry
            }
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Log password reset audit event
    /// </summary>
    private async Task LogAuditEventAsync(
        Guid accountId,
        string action,
        bool success,
        string? partialToken = null,
        string? errorMessage = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = HttpContextUtil.GetClientIpAddress(httpContext);
            var userAgent = HttpContextUtil.GetUserAgent(httpContext);

            var auditLog = new PasswordResetAuditLogs
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                Action = action,
                PartialToken = partialToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success,
                ErrorMessage = errorMessage,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.GetRepository<PasswordResetAuditLogs>()
                .InsertAsync(auditLog);

            // Fire and forget - don't block main flow
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            // Never throw - audit should not break business logic
            _logger.Error(ex, "Error in audit logging");
        }
    }

    #endregion
}