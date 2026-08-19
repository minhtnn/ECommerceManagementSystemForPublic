using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Domain.Constants;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ValidateResetToken;

public class ValidateResetTokenCommandHandler : IRequestHandler<ValidateResetTokenCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    public ValidateResetTokenCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(
        ValidateResetTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>()
            .SingleOrDefaultAsync(
                predicate: x => x.Code.Equals(request.BrandCode)
            );

        if (existedBrand == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Token không hợp lệ."
            };
        }

        if (existedBrand.Status == EBrandStatus.Inactive)
        {
            throw new BadHttpRequestException("Thương hiệu bị tạm dừng! Xin liên hệ tới quản trị viên!");
        }

        #region 1. Find Account

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
                include: x => x.Include(a => a.CustomerAccounts)
                    .ThenInclude(ca => ca.Customer)
                    .Include(a => a.BrandAccounts)
            );


        if (account == null)
        {
            await LogAuditEventAsync(
                accountId: Guid.Empty,
                action: PasswordResetAuditActions.TokenValidated,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Account not found"
            );

            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Token không hợp lệ."
            };
        }

        #endregion

        #region 3. Check Account Locked  ← thêm mới, thiếu trong bản cũ

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

            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = $"Tài khoản tạm thời bị khóa. Vui lòng thử lại sau {remainingMinutes} phút."
            };
        }

        #endregion

        #region 2. Check Token Exists

        if (string.IsNullOrWhiteSpace(account.PasswordResetToken))
        {
            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "No token found"
            );

            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Token không tồn tại. Vui lòng yêu cầu lại."
            };
        }

        #endregion

        #region 3. Check Token Already Used

        if (account.PasswordResetTokenUsed == true)
        {
            await LogAuditEventAsync(
                accountId: account.Id,
                action: PasswordResetAuditActions.FailedAttempt,
                success: false,
                partialToken: AuthenUtil.GetPartialToken(request.Token),
                errorMessage: "Token already used"
            );

            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Token đã được sử dụng. Vui lòng yêu cầu lại."
            };
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

            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Token đã hết hạn. Vui lòng yêu cầu lại."
            };
        }

        #endregion

        #region 4. Validate Token Match (Secure Compare)

        if (!AuthenUtil.SecureCompare(account.PasswordResetToken, request.Token))
        {
            // Increment failed attempts
            account.PasswordResetFailedAttempts = (account.PasswordResetFailedAttempts ?? 0) + 1;

            // Lock account after 5 failed attempts
            if (account.PasswordResetFailedAttempts >= 5)
            {
                account.PasswordResetLockedUntil = DateTime.UtcNow.AddMinutes(30);

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
            }
            else
            {
                await LogAuditEventAsync(
                    accountId: account.Id,
                    action: PasswordResetAuditActions.FailedAttempt,
                    success: false,
                    partialToken: AuthenUtil.GetPartialToken(request.Token),
                    errorMessage: "Token mismatch"
                );
            }

            _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);
            await _unitOfWork.CommitAsync();

            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Token không đúng!"
            };
        }

        #endregion

        #region 6. Token Valid

        await LogAuditEventAsync(
            accountId: account.Id,
            action: PasswordResetAuditActions.TokenValidated,
            success: true,
            partialToken: AuthenUtil.GetPartialToken(request.Token)
        );

        _logger.Information(
            "Password reset token validated for Account: {AccountId}",
            account.Id
        );

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Token hợp lệ."
        };

        #endregion
    }

    #region Private Helper Methods

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
            _logger.Error(ex, "Error in audit logging");
        }
    }

    #endregion
}