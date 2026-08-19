using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;

    public ChangePasswordCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger, IHttpContextAccessor httpContextAccessor, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var accountId = _claimService.GetCurrentAccountId();
        if (role == null || accountId == null)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }
        
        var account = await _unitOfWork.GetRepository<Accounts>().SingleOrDefaultAsync(
            predicate: x => x.Id == accountId,
            include: x => x.Include(a => a.RefreshTokens)
        );
        if (account == null)
        {
            throw new BadHttpRequestException("Không tìm thấy tài khoản!");
        }

        // Kiểm tra account status
        if (account.Status == EAccountStatus.Inactive)
        {
            throw new BadHttpRequestException("Tài khoản đã bị tạm khóa!");
        }
        
        if (string.IsNullOrEmpty(account.PasswordHash) || string.IsNullOrEmpty(account.PasswordSalt))
        {
            throw new BadHttpRequestException("Tài khoản này không sử dụng mật khẩu (đăng nhập qua Google)!");
        }
        
        var isValidPassword = AuthenUtil.Verify(
            request.CurrentPassword,
            account.PasswordHash,
            account.PasswordSalt
        );

        if (!isValidPassword)
        {
            _logger.Warning(
                "Failed password change attempt for account {AccountId} - incorrect current password",
                accountId
            );
            throw new BadHttpRequestException("Mật khẩu hiện tại không đúng!");
        }
        
        var (passwordHash, passwordSalt) = AuthenUtil.HashPassword(request.NewPassword);
        
        account.PasswordHash = passwordHash;
        account.PasswordSalt = passwordSalt;
        account.LastModifiedDate = DateTime.UtcNow;
        
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
        
        _unitOfWork.GetRepository<Accounts>().UpdateAsync(account);
        
        if (account.RefreshTokens != null && account.RefreshTokens.Any())
        {
            foreach (var token in account.RefreshTokens.Where(t => !t.IsRevoked))
            {
                token.IsRevoked = true;
                token.RevokedDate = DateTime.UtcNow;
                token.LastModifiedDate = DateTime.UtcNow;
            }
            _unitOfWork.GetRepository<RefreshTokens>().UpdateRange(account.RefreshTokens);
        }
        
        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Không thể đổi mật khẩu: {commitResult.Message}");
        }

        _logger.Information(
            "Password changed successfully for account {AccountId} ({Role})",
            accountId,
            account.Role
        );
        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại."
        };
    }
}