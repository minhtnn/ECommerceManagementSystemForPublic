using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResendOTP.Email;

public class ResendEmailOTPCommandHandler : IRequestHandler<ResendEmailOTPCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    private readonly IMediaService _mediaService;
    private const int RESEND_COOLDOWN_SECONDS = 30;

    public ResendEmailOTPCommandHandler(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        // IEmailService emailService,
        ILogger logger, IEmailService emailService, IMediaService mediaService)
    {
        _unitOfWork = unitOfWork;
        // _emailService = emailService;
        _logger = logger;
        _emailService = emailService;
        _mediaService = mediaService;
    }

    public async ValueTask<ApiResponse> Handle(ResendEmailOTPCommand request, CancellationToken cancellationToken)
    {
        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>().SingleOrDefaultAsync(
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

        // 1. Kiểm tra account có tồn tại không
        var existingAccount = await _unitOfWork.GetRepository<Accounts>().SingleOrDefaultAsync(
            predicate: x => x.Email.Equals(request.Email)
                            && x.CustomerAccounts.Any(x => x.Customer.BrandId.Equals(existedBrand.Id)),
            include: x => x.Include(x => x.CustomerAccounts)
                .ThenInclude(x => x.Customer)
        );

        if (existingAccount == null)
        {
            throw new BadHttpRequestException("Email không tồn tại trong hệ thống!");
        }

        // 2. Kiểm tra account đã được verify chưa
        if (existingAccount.IsEmailVerified != null && existingAccount.IsEmailVerified == true)
        {
            throw new BadHttpRequestException("Tài khoản đã được xác thực!");
        }

        // 3. Rate limit - ĐÚNG CÁCH: kiểm tra thời gian gửi OTP lần cuối
        if (existingAccount.LastOtpSentAt != null)
        {
            var timeSinceLastSend = DateTime.UtcNow - existingAccount.LastOtpSentAt.Value;
            var remainingSeconds = RESEND_COOLDOWN_SECONDS - (int)timeSinceLastSend.TotalSeconds;

            if (remainingSeconds > 0)
            {
                throw new BadHttpRequestException(
                    $"Vui lòng đợi {remainingSeconds} giây trước khi gửi lại OTP!"
                );
            }
        }

        // 4. Tạo OTP mới
        var newOtpCode = AuthenUtil.CreateOtpVerification();
        var newOtpExpiry = AuthenUtil.CreateOtpExpired();

        existingAccount.EmailVerificationToken = newOtpCode;
        existingAccount.EmailVerificationTokenExpiry = newOtpExpiry;
        existingAccount.LastOtpSentAt = DateTime.UtcNow;

        // 5. Cập nhật database
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

        var commitResult = await _unitOfWork.CommitTransactionAsync();
        if (!commitResult.IsSuccess)
        {
            _logger.Error($"Transaction failed: {commitResult.Message}", commitResult.Exception);
            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception("Không thể gửi lại mã OTP!");
        }

        try
        {
            string logoBase64String = null;
            if (!string.IsNullOrWhiteSpace(existedBrand.LogoUrl))
            {
                try
                {
                    logoBase64String = await _mediaService.GetImageUrlAsync(
                        existedBrand.LogoUrl,
                        TimeSpan.FromHours(2)
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to get image url", ex);
                }
            }

            var sendOtpEmailRequest = new SendOtpEmailRequest()
            {
                BrandLogoBase64 = logoBase64String,
                BrandName = existedBrand.Name,
                CustomerName = existingAccount.CustomerAccounts[0]?.Customer.FullName ?? existingAccount.Username,
                ToEmail = existingAccount.Email,
                FromEmail = existedBrand.Email,
                OtpCode = existingAccount.EmailVerificationToken,
                ExpiredTime = AuthenUtil.OtpExpired,
                TimeMeasureUnit = "phút"
            };

            var emailResult = await _emailService.SendEmailVerificationAsync(
                apiKey: brandSetting.SendGridApiKey,
                fromEmail: brandSetting.SendGridFromEmail,
                fromName: brandSetting.SendGridFromName,
                mainColor: "#ed1c24",
                sendOtpEmailRequest,
                cancellationToken
            );

            if (!emailResult.IsSuccess)
            {
                _logger.Warning(
                    "Account created but email sending failed: {Error}",
                    emailResult.Message
                );
            }
        }
        catch (Exception emailEx)
        {
            _logger.Error(emailEx, "Exception while sending verification email");
            // Don't fail the registration
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Đã gửi lại mã OTP đến email của bạn!",
            Data = new { Email = existingAccount.Email }
        };
    }
}