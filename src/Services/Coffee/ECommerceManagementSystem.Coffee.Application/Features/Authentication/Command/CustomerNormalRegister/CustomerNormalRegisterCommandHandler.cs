using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerNormalRegister;

public class CustomerNormalRegisterCommandHandler : IRequestHandler<CustomerNormalRegisterCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IMediaService _mediaService;
    private readonly ILogger _logger;
    private readonly IEmailService _emailService;

    public CustomerNormalRegisterCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ICacheInvalidationService cacheInvalidation, IMediaService mediaService, ILogger logger,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _cacheInvalidation = cacheInvalidation;
        _mediaService = mediaService;
        _logger = logger;
        _emailService = emailService;
    }

    public async ValueTask<ApiResponse> Handle(CustomerNormalRegisterCommand request,
        CancellationToken cancellationToken)
    {
        #region Check account logic

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

        var isBrandAdminEmail = await _unitOfWork.GetRepository<Accounts>()
            .AnyAsync(
                predicate: x => x.Email == request.Email &&
                                x.Role == ERole.BrandAdmin
            );

        if (isBrandAdminEmail)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "Email này không hợp lệ. Vui lòng sử dụng email khác!"
            };
        }

        var isBrandAdminUsername = await _unitOfWork.GetRepository<Accounts>()
            .AnyAsync(
                predicate: x => x.Username == request.Username &&
                                x.Role == ERole.BrandAdmin
            );

        if (isBrandAdminUsername)
        {
            throw new BadHttpRequestException(
                "Tên đăng nhập này thuộc tài khoản quản trị thương hiệu. Vui lòng sử dụng tên đăng nhập khác!"
            );
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

        var existingAccounts = await _unitOfWork.GetRepository<Accounts>()
            .GetListAsync(
                predicate: x => x.Email == request.Email ||
                                x.Username == request.Username ||
                                x.PhoneNumber == request.PhoneNumber,
                include: x => x.Include(a => a.CustomerAccounts)
                    .ThenInclude(ca => ca.Customer)
            );
        if (existingAccounts != null && existingAccounts.Any())
        {
            foreach (var existingAccount in existingAccounts)
            {
                // Kiểm tra xem có CustomerAccount nào thuộc Brand này không
                var customerInSameBrand = existingAccount.CustomerAccounts
                    ?.FirstOrDefault(ca => ca.Customer.BrandId == existedBrand.Id);

                if (customerInSameBrand != null)
                {
                    // Kiểm tra trùng từng trường cụ thể
                    if (existingAccount.Email == request.Email)
                    {
                        throw new BadHttpRequestException(
                            "Email này đã được đăng ký cho thương hiệu này!"
                        );
                    }

                    if (existingAccount.Username == request.Username)
                    {
                        throw new BadHttpRequestException(
                            "Tên đăng nhập này đã được sử dụng trong thương hiệu này!"
                        );
                    }

                    if (existingAccount.PhoneNumber == request.PhoneNumber)
                    {
                        throw new BadHttpRequestException(
                            "Số điện thoại này đã được đăng ký cho thương hiệu này!"
                        );
                    }
                }
            }

            // OPTION: Nếu bạn muốn Username unique toàn hệ thống (cross-brand)
            var usernameExists = existingAccounts.Any(a => a.Username == request.Username);
            if (usernameExists)
            {
                throw new BadHttpRequestException(
                    "Tên đăng nhập đã được sử dụng trong hệ thống!"
                );
            }
        }

        #endregion

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

        #region Create account and relevant information

        var (passwordHash, passwordSalt) = AuthenUtil.HashPassword(request.PasswordString);
        var account = new Accounts()
        {
            Id = Guid.CreateVersion7(),
            Role = ERole.EndCustomer,
            PhoneNumber = request.PhoneNumber,
            IsPhoneVerified = false,
            Email = request.Email,
            Username = request.Username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Status = EAccountStatus.EmailVerifyPending,
            IsEmailVerified = false,
            EmailVerificationToken = AuthenUtil.CreateOtpVerification(),
            EmailVerificationTokenExpiry = AuthenUtil.CreateOtpExpired(),
            LastOtpSentAt = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            AuthProvider = EAuthProvider.Local,
        };
        var customer = new Domain.Entities.Customers()
        {
            Id = Guid.CreateVersion7(),
            BrandId = existedBrand.Id,
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            CreatedDate = DateTime.UtcNow,
        };

        var customerAccount = new CustomerAccounts()
        {
            Id = Guid.CreateVersion7(),
            AccountId = account.Id,
            CustomerId = customer.Id,
            CreatedDate = DateTime.UtcNow
        };

        #endregion

        #region Check if customer avatar are contained in request

        string uploadedFileName = "";

        if (request.Avatar != null)
        {
            try
            {
                if (!ImageUtil.IsValidImageFile(request.Avatar))
                {
                    throw new BadHttpRequestException(
                        $"Avatar không hợp lệ. " +
                        $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                    );
                }

                using var memoryStream = new MemoryStream();
                await request.Avatar.CopyToAsync(memoryStream, cancellationToken);
                var uploadResult = await _mediaService.UploadImageFromFormAsync(
                    request.Avatar,
                    folderPath: nameof(Domain.Entities.Customers)
                        .ToLowerInvariant(),
                    cancellationToken
                );
                if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                {
                    throw new Exception(
                        $"Không thể upload logo: {uploadResult.Message}"
                    );
                }

                uploadedFileName = uploadResult.FileName;
                customer.AvatarUrl = uploadResult.FileName;
            }
            catch (Exception e)
            {
                try
                {
                    await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                    _logger.Information("Deleted rolled back image: {FileName}", uploadedFileName);
                }
                catch (Exception deleteEx)
                {
                    _logger.Error(
                        deleteEx,
                        "Failed to delete image {FileName} during rollback",
                        uploadedFileName
                    );
                }
            }
        }

        #endregion

        #region Start transaction

        await _unitOfWork.GetRepository<Accounts>().InsertAsync(account);
        await _unitOfWork.GetRepository<Domain.Entities.Customers>().InsertAsync(customer);
        await _unitOfWork.GetRepository<CustomerAccounts>().InsertAsync(customerAccount);

        var commitResult = await _unitOfWork.CommitTransactionAsync();

        if (!commitResult.IsSuccess)
        {
            _logger.Error(
                "Transaction commit failed: {Message}. Exception: {Exception}",
                commitResult.Message,
                commitResult.Exception?.Message
            );
            try
            {
                await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                _logger.Information("Deleted rolled back image: {FileName}", uploadedFileName);
            }
            catch (Exception deleteEx)
            {
                _logger.Error(
                    deleteEx,
                    "Failed to delete image {FileName} during rollback",
                    uploadedFileName
                );
            }

            await _unitOfWork.RollbackTransactionAsync();
            throw new Exception($"Không thể tạo tài khoản : {commitResult.Message}");
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
                CustomerName = customer.FullName ?? account.Username,
                ToEmail = customer.Email,
                FromEmail = existedBrand.Email,
                OtpCode = account.EmailVerificationToken,
                ExpiredTime = AuthenUtil.OtpExpired,
                TimeMeasureUnit = "phút"
            };

            var emailResult = await _emailService.SendEmailVerificationAsync(
                brandSetting.SendGridApiKey,
                brandSetting.SendGridFromEmail,
                brandSetting.SendGridFromName,
                brandSetting.MainColor,
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

        // 6. Invalidate cache (sau khi commit thành công)
        // Khách hàng đăng kí chỉ cần invalidate của mỗi thông tin khách hàng đó.
        var cacheResult = await _cacheInvalidation.InvalidateEntityCacheAsync(
            lockKey: CacheConfig.EntityInvalidationLock(
                CacheConfig.EntityDetailCachePrefix(nameof(Domain.Entities.Customers), customer.Id.ToString())),
            operation: EOperationBeforeCache.BulkCreate,
            counterKey: CacheConfig.EntityInvalidationCounter(
                CacheConfig.EntityDetailCachePrefix(nameof(Domain.Entities.Customers), customer.Id.ToString())),
            entityCachePrefix: CacheConfig.EntityDetailCachePrefix(nameof(Domain.Entities.Customers),
                customer.Id.ToString())
        );

        if (cacheResult.Success)
        {
            _logger.Information(
                "Created new customer account (AccountID: {AccountId})," +
                " (CustomerId: {CustomerId})," +
                " (CustomerAccountId: {CustomerAccountId})," +
                " with message: {Message}",
                account.Id,
                customer.Id,
                customerAccount.Id,
                cacheResult.Message
            );
        }
        else
        {
            _logger.Warning(
                "Created new customer account (AccountID: {AccountId})," +
                " (CustomerId: {CustomerId})," +
                " (CustomerAccountId: {CustomerAccountId})," +
                " but cache invalidation failed: {CacheMessage}",
                account.Id,
                customer.Id,
                customerAccount.Id,
                cacheResult.Message,
                cacheResult.Message
            );
        }

        #endregion

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = "Tài khoản khách hàng cần được xác thực email!",
            Data = account.Email,
        };
    }
}