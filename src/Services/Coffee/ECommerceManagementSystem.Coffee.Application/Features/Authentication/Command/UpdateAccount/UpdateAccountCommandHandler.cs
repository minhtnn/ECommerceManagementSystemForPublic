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

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.UpdateAccount;

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand, ApiResponse>
{
    private readonly IClaimService _claimService;
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly ILogger _logger;
    private readonly IEmailService _emailService;


    public UpdateAccountCommandHandler(IClaimService claimService,
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, IMediaService mediaService, ILogger logger,
        IEmailService emailService)
    {
        _claimService = claimService;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _logger = logger;
        _emailService = emailService;
    }

    public async ValueTask<ApiResponse> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var role = _claimService.GetCurrentRoleEnum();
        var accountId = _claimService.GetCurrentAccountId();
        var referenceId = _claimService.GetCurrentReferenceId();
        string uploadedFileName = "";
        string emailVerificationToken = "";
        var isEmailChanged = false;
        int numberOfChange = 0;

        if (accountId == null || referenceId == null || role == null ||
            !(role is ERole.BrandAdmin or ERole.EndCustomer))
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status401Unauthorized,
                Message = "Bạn không có quyền này!"
            };
        }

        if (request.BrandCode == null)
        {
            throw new BadHttpRequestException("Mã thương hiệu không được để trống!");
        }

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

        if (role == ERole.BrandAdmin)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return new ApiResponse()
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "Địa chỉ không được để trống!"
                };
            }

            var brandAccount = await _unitOfWork.GetRepository<BrandAccounts>().SingleOrDefaultAsync(
                predicate: x =>
                    x.AccountId == accountId && x.BrandId == referenceId
                                             && x.Brand.Code == request.BrandCode
                                             && x.Account.Status == EAccountStatus.Active
                                             && x.Brand.Status == EBrandStatus.Active,
                include: x => x.Include(x => x.Account)
                    .Include(x => x.Brand)
            );

            if (brandAccount == null)
            {
                return new ApiResponse()
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Message = "Không tìm thấy tài khoản hoặc tài khoản đang bị vô hiệu hóa!",
                };
            }

            isEmailChanged = brandAccount.Account.Email != request.Email;
            if (isEmailChanged)
            {
                var existedBrandInIf = await _unitOfWork.GetRepository<Domain.Entities.Brands>().SingleOrDefaultAsync(
                    predicate: x => x.Email.Equals(request.Email.Trim())
                );
                if (existedBrandInIf != null)
                {
                    return new ApiResponse()
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Message = "Email thương hiệu không được trùng!"
                    };
                }

                var existedAccountInIf = await _unitOfWork.GetRepository<Accounts>().SingleOrDefaultAsync(
                    predicate: x => (x.Email.Equals(request.Email.Trim()) ||
                                     (x.Email.Equals(request.Email.Trim())))
                );
                if (existedAccountInIf != null)
                {
                    return new ApiResponse()
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Message = "Email thương hiệu không được trùng!"
                    };
                }
            }


            brandAccount.Brand.Name = request.Name;
            brandAccount.Brand.Fullname = request.FullName;
            brandAccount.Account.PhoneNumber = request.PhoneNumber;
            brandAccount.Brand.PhoneNumber = request.PhoneNumber;
            brandAccount.Account.Email = request.Email;
            brandAccount.Brand.Email = request.Email;
            brandAccount.Brand.Address = request.Address;

            if (request.Image != null)
            {
                try
                {
                    if (!ImageUtil.IsValidImageFile(request.Image))
                    {
                        throw new BadHttpRequestException(
                            $"Avatar không hợp lệ. " +
                            $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                        );
                    }

                    using var memoryStream = new MemoryStream();
                    await request.Image.CopyToAsync(memoryStream, cancellationToken);
                    var uploadResult = await _mediaService.UploadImageFromFormAsync(
                        request.Image,
                        folderPath: nameof(Domain.Entities.Brands)
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
                    brandAccount.Brand.LogoUrl = uploadResult.FileName;
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

            _unitOfWork.GetRepository<BrandAccounts>().UpdateAsync(brandAccount);

            ++numberOfChange;
        }
        else if (role == ERole.EndCustomer)
        {
            var customerAccount = await _unitOfWork.GetRepository<CustomerAccounts>().SingleOrDefaultAsync(
                predicate: x =>
                    x.AccountId == accountId && x.CustomerId == referenceId
                                             && x.Customer.Brand.Code == request.BrandCode
                                             && x.Account.Status == EAccountStatus.Active,
                include: x => x.Include(x => x.Account)
                    .Include(x => x.Customer).ThenInclude(x => x.Brand)
            );

            if (customerAccount == null)
            {
                return new ApiResponse()
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Message = "Không tìm thấy tài khoản hoặc tài khoản đang bị vô hiệu hóa!",
                };
            }

            isEmailChanged = customerAccount.Account.Email != request.Email;
            var existingAccounts = await _unitOfWork.GetRepository<Accounts>()
                .GetListAsync(
                    predicate: x => x.Email == request.Email || x.PhoneNumber == request.PhoneNumber,
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

                        if (existingAccount.PhoneNumber == request.PhoneNumber)
                        {
                            throw new BadHttpRequestException(
                                "Số điện thoại này đã được đăng ký cho thương hiệu này!"
                            );
                        }
                    }
                }
            }


            customerAccount.Account.Email = request.Email;
            customerAccount.Account.PhoneNumber = request.PhoneNumber;
            customerAccount.Customer.Email = request.Email;
            customerAccount.Customer.PhoneNumber = request.PhoneNumber;
            customerAccount.Customer.FullName = request.FullName;

            if (request.Image != null)
            {
                try
                {
                    if (!ImageUtil.IsValidImageFile(request.Image))
                    {
                        throw new BadHttpRequestException(
                            $"Avatar không hợp lệ. " +
                            $"Chỉ chấp nhận file .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB"
                        );
                    }

                    using var memoryStream = new MemoryStream();
                    await request.Image.CopyToAsync(memoryStream, cancellationToken);
                    var uploadResult = await _mediaService.UploadImageFromFormAsync(
                        request.Image,
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
                    customerAccount.Customer.AvatarUrl = uploadResult.FileName;
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

            if (isEmailChanged)
            {
                emailVerificationToken = AuthenUtil.CreateOtpVerification();
                customerAccount.Account.Status = EAccountStatus.EmailVerifyPending;
                customerAccount.Account.IsEmailVerified = false;
                customerAccount.Account.EmailVerificationToken = emailVerificationToken;
                customerAccount.Account.EmailVerificationTokenExpiry = AuthenUtil.CreateOtpExpired();
                customerAccount.Account.LastModifiedDate = DateTime.UtcNow;
            }

            _unitOfWork.GetRepository<CustomerAccounts>().UpdateAsync(customerAccount);
            ++numberOfChange;
        }

        var transactionResult = await _unitOfWork.BeginTransactionAsync();
        if (!transactionResult.IsSuccess)
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Không thể bắt đầu transaction"
            };
        }

        if (numberOfChange > 0)
        {
            var commitResult = await _unitOfWork.CommitTransactionAsync();

            if (!commitResult.IsSuccess)
            {
                try
                {
                    if (!string.IsNullOrEmpty(uploadedFileName))
                    {
                        await _mediaService.DeleteFileAsync(uploadedFileName, cancellationToken);
                    }
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
        }

        if (isEmailChanged)
        {
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
                    CustomerName = request.FullName ?? "Khách hàng",
                    ToEmail = request.Email,
                    FromEmail = existedBrand.Email,
                    OtpCode = emailVerificationToken,
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
        }

        return new ApiResponse()
        {
            Status = StatusCodes.Status201Created,
            Message = isEmailChanged
                ? "Tài khoản khách hàng cần được xác thực email!"
                : "Thay đổi tài khoản thành công! Thông tin sẽ được thay đổi trong ít phút nữa!",
            Data = isEmailChanged ? request.Email : accountId,
        };
    }
}