using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.UpdateProductCategory;

public class UpdateProductCategoryCommandValidator : AbstractValidator<UpdateProductCategoryCommand>
{
    public UpdateProductCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id của danh mục không được để trống!");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục không được để trống!")
            .MaximumLength(200).WithMessage("Tên danh mục không dài quá 200 kí tự!")
            .MinimumLength(2).WithMessage("Tên danh mục không ít hơn 2 kí tự!");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả danh mục không dài quá 1000 kí tự!");
        RuleFor(x => x.DisplayOrder);
        // RuleFor(x => x.ImageUrl);
        RuleFor(x => x.Status);
    }
}