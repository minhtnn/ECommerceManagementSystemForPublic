using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Carts.Command.UpdateCart;

public class UpdateCartCommandValidator : AbstractValidator<UpdateCartCommand>
{
    private const int MaxItemsPerCart = 50; // Giới hạn tối đa 50 sản phẩm/giỏ
    private const int MaxQuantityPerItem = 999; // Tối đa 999 sản phẩm/item

    public UpdateCartCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("Danh sách sản phẩm không được để trống")
            .NotEmpty()
            .WithMessage("Giỏ hàng phải có ít nhất một sản phẩm");

        RuleFor(x => x.Items)
            .Must(items => items == null || items.Count <= MaxItemsPerCart)
            .WithMessage($"Giỏ hàng chỉ được chứa tối đa {MaxItemsPerCart} sản phẩm")
            .When(x => x.Items != null);

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                // ProductId
                item.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("Mã sản phẩm không được để trống")
                    .NotEqual(Guid.Empty)
                    .WithMessage("Mã sản phẩm không hợp lệ");

                // Quantity
                item.RuleFor(i => i.Quantity)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Số lượng sản phẩm phải >= 0")
                    .LessThanOrEqualTo(MaxQuantityPerItem)
                    .WithMessage($"Số lượng sản phẩm không được vượt quá {MaxQuantityPerItem}");
                
            })
            .When(x => x.Items != null && x.Items.Any());

        RuleFor(x => x.Items)
            .Must(items => items == null || 
                  items.Where(i => i.Quantity > 0) // Chỉ check items có quantity > 0
                       .Select(i => i.ProductId)
                       .Distinct()
                       .Count() == items.Count(i => i.Quantity > 0))
            .WithMessage("Không được thêm cùng một sản phẩm nhiều lần. Vui lòng điều chỉnh số lượng!")
            .When(x => x.Items != null && x.Items.Any());

        RuleFor(x => x.CustomerNote)
            .MaximumLength(500)
            .WithMessage("Ghi chú không quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.CustomerNote));

        RuleFor(x => x.AppliedPromotions)
            .Must(promos => promos == null || promos.Count <= 10)
            .WithMessage("Chỉ được áp dụng tối đa 10 mã khuyến mãi")
            .When(x => x.AppliedPromotions != null);

        RuleForEach(x => x.AppliedPromotions)
            .ChildRules(promo =>
            {
                // PromotionId
                promo.RuleFor(p => p.PromotionRuleId)
                    .NotEmpty()
                    .WithMessage("Mã khuyến mãi không được để trống")
                    .NotEqual(Guid.Empty)
                    .WithMessage("Mã khuyến mãi không hợp lệ");

                promo.RuleFor(p => p.PromotionRuleNameSnapshot)
                    .NotEmpty()
                    .WithMessage("Tên khuyến mãi không được để trống")
                    .MaximumLength(200)
                    .WithMessage("Tên khuyến mãi không quá 200 ký tự");

                // DiscountAmountApplied
                promo.RuleFor(p => p.DiscountAmountApplied)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Số tiền giảm giá phải >= 0")
                    .LessThanOrEqualTo(999_999_999)
                    .WithMessage("Số tiền giảm giá không hợp lệ (quá lớn)");
            })
            .When(x => x.AppliedPromotions != null && x.AppliedPromotions.Any());

        RuleFor(x => x.AppliedPromotions)
            .Must(promos => promos == null || 
                  promos.Select(p => p.PromotionRuleId).Distinct().Count() == promos.Count)
            .WithMessage("Không được áp dụng cùng một mã khuyến mãi nhiều lần")
            .When(x => x.AppliedPromotions != null && x.AppliedPromotions.Any());

        // Validate giỏ hàng phải có ít nhất 1 item sau khi lọc quantity = 0
        RuleFor(x => x.Items)
            .Must(items => items == null || items.Any(i => i.Quantity > 0))
            .WithMessage("Giỏ hàng phải có ít nhất một sản phẩm với số lượng > 0")
            .When(x => x.Items != null);
    }
}