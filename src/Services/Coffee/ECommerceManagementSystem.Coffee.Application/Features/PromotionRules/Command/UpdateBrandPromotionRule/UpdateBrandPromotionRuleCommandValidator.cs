using ECommerceManagementSystem.Coffee.Domain.Enums;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.UpdateBrandPromotionRule;

public class UpdateBrandPromotionRuleCommandValidator : AbstractValidator<UpdateBrandPromotionRuleCommand>
{
    public UpdateBrandPromotionRuleCommandValidator()
    {
        // ─── Id ──────────────────────────────────────────────────────
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id khuyến mãi không được để trống");

        // ─── Thông tin cơ bản (optional) ────────────────────────────
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên khuyến mãi không được để trống khi truyền lên")
            .MaximumLength(50).WithMessage("Tên khuyến mãi không quá 50 ký tự")
            .When(x => x.Name != null);

        RuleFor(x => x.ShortDescription)
            .MaximumLength(100).WithMessage("Mô tả ngắn không quá 100 ký tự")
            .When(x => x.ShortDescription != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả không quá 500 ký tự")
            .When(x => x.Description != null);

        // ─── PromotionType ───────────────────────────────────────────
        RuleFor(x => x.PromotionType)
            .IsInEnum().WithMessage("PromotionType không hợp lệ")
            .When(x => x.PromotionType.HasValue);

        // ─── Priority ────────────────────────────────────────────────
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority phải >= 0")
            .When(x => x.Priority.HasValue);

        // ─── GlobalDiscountCap ───────────────────────────────────────
        // 0 = xóa cap (set null), > 0 = set cap mới
        RuleFor(x => x.GlobalDiscountCap)
            .GreaterThanOrEqualTo(0).WithMessage("GlobalDiscountCap phải >= 0 (0 = xóa cap)")
            .When(x => x.GlobalDiscountCap.HasValue);

        // ─── Status ──────────────────────────────────────────────────
        RuleFor(x => x.Status)
            .Must(s => s == EPromotionStatus.Active || s == EPromotionStatus.Inactive)
            .WithMessage("Chỉ được cập nhật Status thành Active hoặc Inactive")
            .When(x => x.Status.HasValue);

        // ─── Dates ───────────────────────────────────────────────────
        RuleFor(x => x.StartDate)
            .Must(d => d > DateTime.UtcNow)
            .WithMessage("Ngày bắt đầu phải là thời điểm trong tương lai")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate)
            .Must(d => d > DateTime.UtcNow)
            .WithMessage("Ngày kết thúc phải là thời điểm trong tương lai")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate < x.EndDate)
            .WithMessage("Ngày bắt đầu phải trước ngày kết thúc")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        // ─── Conditions (khi truyền lên → Replace All) ──────────────
        RuleFor(x => x.RuleConditions)
            .NotEmpty().WithMessage("Danh sách conditions không được rỗng khi truyền lên")
            .When(x => x.RuleConditions != null);

        RuleForEach(x => x.RuleConditions)
            .ChildRules(c =>
            {
                c.RuleFor(x => x.ConditionType)
                    .IsInEnum().WithMessage("ConditionType không hợp lệ");
                c.RuleFor(x => x.Operator)
                    .IsInEnum().WithMessage("Operator không hợp lệ");
                c.RuleFor(x => x.Value)
                    .NotEmpty().WithMessage("Giá trị điều kiện không được để trống");
            })
            .When(x => x.RuleConditions != null && x.RuleConditions.Any());

        // ─── Actions (khi truyền lên → Replace All) ─────────────────
        RuleFor(x => x.RuleActions)
            .NotEmpty().WithMessage("Danh sách actions không được rỗng khi truyền lên")
            .When(x => x.RuleActions != null);

        RuleForEach(x => x.RuleActions)
            .ChildRules(a =>
            {
                a.RuleFor(x => x.ActionType)
                    .IsInEnum().WithMessage("ActionType không hợp lệ");

                a.RuleFor(x => x.MaxDiscountAmountForPercentage)
                    .GreaterThan(0).WithMessage("MaxDiscountAmountForPercentage phải > 0")
                    .When(x => x.MaxDiscountAmountForPercentage.HasValue);

                a.RuleForEach(x => x.RuleActionTargets)
                    .ChildRules(t =>
                    {
                        t.RuleFor(x => x.TargetType)
                            .IsInEnum().WithMessage("TargetType không hợp lệ");

                        // Role: không được là BuyProduct (đã bỏ)
                        t.RuleFor(x => x.Role)
                            .IsInEnum().WithMessage("Role không hợp lệ")
                            .Must(r => r != EActionTargetRole.BuyProduct)
                            .WithMessage("Role 'BuyProduct' không còn được sử dụng. " +
                                         "Thông tin sản phẩm cần mua phải khai báo trong điều kiện 'MinQuantityOfProduct'");

                        t.RuleFor(x => x.TargetId)
                            .NotEmpty().WithMessage("TargetId không được để trống");

                        t.RuleFor(x => x.Quantity)
                            .GreaterThanOrEqualTo(1).WithMessage("Quantity phải >= 1");
                    })
                    .When(x => x.RuleActionTargets != null && x.RuleActionTargets.Any());
            })
            .When(x => x.RuleActions != null && x.RuleActions.Any());
    }
}