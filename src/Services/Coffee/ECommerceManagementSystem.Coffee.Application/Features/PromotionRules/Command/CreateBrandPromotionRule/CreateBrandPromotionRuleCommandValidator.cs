using ECommerceManagementSystem.Coffee.Domain.Enums;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.CreateBrandPromotionRule;

public class CreateBrandPromotionRuleCommandValidator : AbstractValidator<CreateBrandPromotionRuleCommand>
{
    public CreateBrandPromotionRuleCommandValidator()
    {
        // ─── Thông tin cơ bản ────────────────────────────────────────
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã khuyến mãi không được để trống")
            .MaximumLength(100).WithMessage("Mã khuyến mãi không quá 100 ký tự");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên khuyến mãi không được để trống")
            .MaximumLength(200).WithMessage("Tên khuyến mãi không quá 200 ký tự");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(100).WithMessage("Mô tả ngắn không quá 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.ShortDescription));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // ─── PromotionType ───────────────────────────────────────────
        RuleFor(x => x.PromotionType)
            .IsInEnum().WithMessage("PromotionType không hợp lệ");

        // ─── GlobalDiscountCap ───────────────────────────────────────
        RuleFor(x => x.GlobalDiscountCap)
            .GreaterThan(0).WithMessage("GlobalDiscountCap phải lớn hơn 0")
            .When(x => x.GlobalDiscountCap != 0);

        // FreeShipping không được có GlobalDiscountCap — check thêm ở Handler
        RuleFor(x => x.GlobalDiscountCap)
            .Must(cap => cap == 0)
            .WithMessage("FreeShipping promotion không được khai báo GlobalDiscountCap")
            .When(x => x.PromotionType == EPromotionType.FreeShipping && x.GlobalDiscountCap != 0);

        // ─── Priority ────────────────────────────────────────────────
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority phải >= 0")
            .When(x => x.Priority != 0);

        // ─── Dates ───────────────────────────────────────────────────
        RuleFor(x => x.StartDate)
            .NotNull().WithMessage("Ngày bắt đầu không được để trống");

        RuleFor(x => x.EndDate)
            .NotNull().WithMessage("Ngày kết thúc không được để trống");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate < x.EndDate)
            .WithMessage("Ngày bắt đầu phải trước ngày kết thúc");

        // ─── Conditions ──────────────────────────────────────────────
        RuleFor(x => x.RuleConditions)
            .NotNull().NotEmpty().WithMessage("Khuyến mãi phải có ít nhất 1 điều kiện");

        RuleForEach(x => x.RuleConditions)
            .ChildRules(condition =>
            {
                condition.RuleFor(c => c.ConditionType)
                    .IsInEnum().WithMessage("ConditionType không hợp lệ");

                condition.RuleFor(c => c.Operator)
                    .IsInEnum().WithMessage("Operator không hợp lệ");

                condition.RuleFor(c => c.Value)
                    .NotEmpty().When(x => x.ConditionType != ERuleConditionType.FirstOrder).WithMessage("Giá trị điều kiện không được để trống");
            })
            .When(x => x.RuleConditions != null);

        // ─── Actions ─────────────────────────────────────────────────
        RuleFor(x => x.RuleActions)
            .NotNull().NotEmpty().WithMessage("Khuyến mãi phải có ít nhất 1 action");

        RuleForEach(x => x.RuleActions)
            .ChildRules(action =>
            {
                action.RuleFor(a => a.ActionType)
                    .IsInEnum().WithMessage("ActionType không hợp lệ");

                action.RuleFor(a => a.MaxDiscountAmountForPercentage)
                    .GreaterThan(0).WithMessage("MaxDiscountAmountForPercentage phải > 0")
                    .When(a => a.MaxDiscountAmountForPercentage != 0);

                action.RuleForEach(a => a.RuleActionTargets)
                    .ChildRules(target =>
                    {
                        target.RuleFor(t => t.TargetType)
                            .IsInEnum().WithMessage("TargetType không hợp lệ");

                        // Role: không được là BuyProduct (đã bỏ)
                        target.RuleFor(t => t.Role)
                            .IsInEnum().WithMessage("Role không hợp lệ")
                            .Must(r => r != EActionTargetRole.BuyProduct)
                            .WithMessage("Role 'BuyProduct' không còn được sử dụng. " +
                                         "Thông tin sản phẩm cần mua phải khai báo trong điều kiện 'MinQuantityOfProduct'");

                        target.RuleFor(t => t.TargetId)
                            .NotEmpty().WithMessage("TargetId không được để trống");

                        target.RuleFor(t => t.Quantity)
                            .GreaterThanOrEqualTo(1).WithMessage("Quantity phải >= 1");
                    })
                    .When(a => a.RuleActionTargets != null && a.RuleActionTargets.Any());
            })
            .When(x => x.RuleActions != null);
    }
}