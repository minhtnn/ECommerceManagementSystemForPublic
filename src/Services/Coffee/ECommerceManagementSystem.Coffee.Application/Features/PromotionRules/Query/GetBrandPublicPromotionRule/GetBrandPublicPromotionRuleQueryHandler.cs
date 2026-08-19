using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPublicPromotionRule;

public class GetBrandPublicPromotionRuleQueryHandler : IRequestHandler<GetBrandPublicPromotionRuleQuery, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;

    public GetBrandPublicPromotionRuleQueryHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async ValueTask<ApiResponse> Handle(GetBrandPublicPromotionRuleQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BrandCode))
        {
            throw new BadHttpRequestException("Brand Code không được để trống!");
        }

        var promotionRules = await _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
            .GetListAsync(
                predicate: x =>
                    x.Brand.Status == EBrandStatus.Active &&
                    ((string.IsNullOrWhiteSpace(request.Code) || x.Code.Contains(x.Code))
                     && ((string.IsNullOrWhiteSpace(request.Name) || x.Code.Contains(x.Name))
                         && x.Brand.Code == request.BrandCode)),
                include: x => x.Include(x => x.Brand)
            );

        return new ApiResponse()
        {
            Status = StatusCodes.Status200OK,
            Message = "Lấy danh sách khuyến mãi thành công!",
            Data = promotionRules
        };
    }
}