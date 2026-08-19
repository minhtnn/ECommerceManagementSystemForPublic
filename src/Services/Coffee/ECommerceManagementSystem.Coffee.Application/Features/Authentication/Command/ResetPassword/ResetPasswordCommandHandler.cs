using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;

    public ResetPasswordCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        #region 1. Validate Brand

        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.BrandCode)
            );

        if (existedBrand == null)
        {
            throw new BadHttpRequestException("Thông tin không hợp lệ!");
        }

        if (existedBrand.Status == EBrandStatus.Inactive)
        {
            throw new BadHttpRequestException("Thương hiệu bị tạm dừng! Xin liên hệ tới quản trị viên!");
        }
        
        var brandSetting = SettingUtil.Parse<BrandSetting>(existedBrand.Configuration);

        #endregion

        #region 2. Find Account (có Brand scope)

        var account = await _unitOfWork.GetRepository<Accounts>()
            .SingleOrDefaultAsync(
                predicate: x =>
                    x.Email == request.Email &&
                    (
                        (x.Role == ERole.EndCustomer &&
                         x.CustomerAccounts.Any(ca => ca.Customer.BrandId == existedBrand.Id)) ||
                        (x.Role == ERole.BrandAdmin &&
                         x.BrandAccounts.Any(ba => ba.BrandId == existedBrand.Id)) ||
                        (x.Role == ERole.SystemAdmin)
                    ),
                include: x => x.Include(a => a.RefreshTokens)
                    .Include(a => a.CustomerAccounts)
                    .ThenInclude(ca => ca.Customer)
                    .ThenInclude(c => c.Brand)
                    .Include(a => a.BrandAccounts)
            );

        if (account == null)
        {
            await LogAuditEventAsync(
                accountId: Guid.Empty,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Account not found"
            );

            throw new BadHttpRequestException("Thông tin không hợp lệ!");
        }

        #endregion

        #region 2. Check Account Locked

        if (account.PasswordResetLockedUntil.HasValue &&
            account.PasswordResetLockedUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(account.PasswordResetLockedUntil.Value - DateTime.UtcNow).TotalMinutes;

            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Account locked"
            );

            throw new BadHttpRequestException(
                $"Tài khoản tạm thời bị khóa. Vui lòng thử lại sau {remainingMinutes} phút."
            );
        }

        #endregion

        #region 3. Validate Token Exists

        if (string.IsNullOrWhiteSpace(account.PasswordResetToken))
        {
            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "No token found"
            );

            throw new BadHttpRequestException(
                "Không tìm thấy token. Vui lòng yêu cầu lại!"
            );
        }

        #endregion

        #region 4. Check Token Already Used

        if (account.PasswordResetTokenUsed == true)
        {
            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Token already used"
            );

            throw new BadHttpRequestException(
                "Token đã được sử dụng. Vui lòng yêu cầu lại!"
            );
        }

        #endregion

        #region 6. Check Token Expiry  ← đổi lên TRƯỚC SecureCompare

        if (!account.PasswordResetTokenExpiry.HasValue ||
            account.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.TokenExpired,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Token expired"
            );

            throw new BadHttpRequestException("Token đã hết hạn. Vui lòng yêu cầu lại!");
        }

        #endregion

        #region 5. Validate Token Match (Secure Compare)

        if (!AuthenUtil.SecureCompare(account.PasswordResetToken, request.Token))
        {
            // Increment failed attempts
            account.PasswordResetFailedAttempts = (account.PasswordResetFailedAttempts ?? 0) + 1;

            // Lock account after 5 failed attempts
            if (account.PasswordResetFailedAttempts >= 5)
            {
                account.PasswordResetLockedUntil = DateTime.UtcNow.AddMinutes(30);

                _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);
                await _unitOfWork.CommitAsync();

                await LogAuditEventAsync(
                    accountId: account.Id,
                    action: PasswordResetAuditActions.AccountLocked,
                    success: false,
                    partialToken: AuthenUtil.GetPartialToken(request.Token),
                    errorMessage: "Too many failed attempts"
                );

                _logger.Warning(
                    "Account {AccountId} locked due to too many failed reset attempts",
                    account.Id
                );

                throw new BadHttpRequestException(
                    "Quá nhiều lần thử sai. Tài khoản đã bị khóa 30 phút!"
                );
            }

            _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);
            await _unitOfWork.CommitAsync();

            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Token mismatch"
            );

            throw new BadHttpRequestException("Token không đúng!");
        }

        #endregion

        #region 7. Reset Password (Transaction)

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            _logger.Error("Failed to begin transaction: {Message}", transactionResult.Message);
            throw new Exception("Không thể bắt đầu transaction");
        }

        try
        {
            // Hash new password
            var (passwordHash, passwordSalt) = AuthenUtil.HashPassword(request.NewPassword);

            // Update account
            account.PasswordHash = passwordHash;
            account.PasswordSalt = passwordSalt;
            account.PasswordResetToken = null; // Clear token
            account.PasswordResetTokenExpiry = null;
            account.PasswordResetTokenUsed = true; // Mark as used
            account.PasswordResetTokenUsedAt = DateTime.UtcNow;
            account.PasswordResetFailedAttempts = 0; // Reset failed attempts
            account.PasswordResetLockedUntil = null; // Unlock account
            account.PasswordLastChangedAt = DateTime.UtcNow;
            account.PasswordChangedCount = (account.PasswordChangedCount ?? 0) + 1;
            account.LastModifiedDate = DateTime.UtcNow;

            // Set email verified if was pending
            if (!account.IsEmailVerified.HasValue || !account.IsEmailVerified.Value)
            {
                account.IsEmailVerified = true;
                account.EmailVerifiedDate = DateTime.UtcNow;
            }

            // Update status to Active if was EmailVerifyPending
            if (account.Status == EAccountStatus.EmailVerifyPending)
            {
                account.Status = EAccountStatus.Active;
            }

            _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);

            // Revoke ALL refresh tokens for security
            var activeTokens = account.RefreshTokens.Where(t => !t.IsRevoked).ToList();
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedDate = DateTime.UtcNow;
                token.LastModifiedDate = DateTime.UtcNow;
            }

            if (activeTokens.Any())
            {
                _unitOfWork.GetRepository<RefreshTokens>().UpdateRange(activeTokens);
            }

            var commitResult = await _unitOfWork.CommitTransactionAsync();

            if (!commitResult.IsSuccess)
            {
                _logger.Error(
                    "Transaction commit failed: {Message}",
                    commitResult.Message
                );
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Không thể đặt lại mật khẩu");
            }

            _logger.Information(
                "Password reset successful for Account: {AccountId}, Email: {Email}",
                account.Id,
                account.Email
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error resetting password for {Email}", request.Email);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        #endregion


        #region 8. Send Confirmation Email

        if (brandSetting.EnabledSendEmailFunction)
        {
            if (!(string.IsNullOrWhiteSpace(brandSetting.SendGridApiKey) ||
                  string.IsNullOrWhiteSpace(brandSetting.SendGridFromEmail) ||
                  string.IsNullOrWhiteSpace(brandSetting.SendGridFromName)))
            {
                try
                {
                    var httpContext = _httpContextAccessor.HttpContext;
                    var ipAddress = HttpContextUtil.GetClientIpAddress(httpContext);

                    var emailResult = await _emailService.SendPasswordChangeNotificationAsync(
                        apiKey: brandSetting.SendGridApiKey,
                        fromEmail: brandSetting.SendGridFromEmail,
                        fromName: brandSetting.SendGridFromName,
                        mainColor: brandSetting.MainColor ?? "#000000",
                        toEmail: account.Email,
                        customerName: account.CustomerAccounts.FirstOrDefault()?.Customer?.FullName ??
                                      account.Username ?? "Khách hàng",
                        brandName: existedBrand.Name,
                        changeTime: DateTime.UtcNow,
                        ipAddress: ipAddress,
                        cancellationToken: cancellationToken
                    );

                    if (!emailResult.IsSuccess)
                    {
                        _logger.Warning(
                            "Failed to send password change notification to {Email}: {Error}",
                            account.Email,
                            emailResult.Message
                        );
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.Error(emailEx, "Exception sending password change notification");
                }
            }
        }

        #endregion

        #region 9. Log Audit Event

        await LogAuditEventAsync(
            accountId: account.Id,
            action: PasswordResetAuditActions.PasswordReset,
            success: true,
            partialToken: AuthenUtil.GetPartialToken(request.Token)
        );

        #endregion

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới."
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