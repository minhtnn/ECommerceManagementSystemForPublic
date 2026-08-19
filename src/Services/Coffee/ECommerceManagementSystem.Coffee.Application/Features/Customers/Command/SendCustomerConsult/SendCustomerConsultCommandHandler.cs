using ECommerceManagementSystem.Coffee.Application.Common.Utils;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Customers.Command.SendCustomerConsult;

public class SendCustomerConsultCommandHandler : IRequestHandler<SendCustomerConsultCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;

    public SendCustomerConsultCommandHandler(IEmailService emailService,
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger)
    {
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(SendCustomerConsultCommand request, CancellationToken cancellationToken)
    {
        var existedBrand = await _unitOfWork.GetRepository<Domain.Entities.Brands>().SingleOrDefaultAsync(
            predicate: x => x.Code.Equals(request.BrandCode)
        );
        if (existedBrand == null)
        {
            throw new BadHttpRequestException("Thương hiệu không tồn tại!");
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

        var emailResult = await _emailService.SendEmailConsultantAsync(
            apiKey: brandSetting.SendGridApiKey,
            fromEmail: brandSetting.SendGridFromEmail,
            fromName: brandSetting.SendGridFromName,
            customerFullName: request.CustomerFullName,
            customerEmail: request.CustomerEmail,
            customerPhone: request.CustomerPhone,
            customerMessage: request.CustomerMessage,
            cancellationToken
        );
        if (!emailResult.IsSuccess)
        {
            _logger.Warning(
                "Account created but email sending failed: {Error}",
                emailResult.Message
            );
            return new ApiResponse()
            {
                Status = StatusCodes.Status201Created,
                Message = "Lỗi gửi thông tin đăng kí tư vấn!",
            };
        }
        else
        {
            return new ApiResponse()
            {
                Status = StatusCodes.Status201Created,
                Message = "Gửi thông tin đăng kí tư vấn thành công!",
            };
        }
        
    }
}