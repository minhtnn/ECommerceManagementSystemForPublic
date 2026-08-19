using ECommerceManagementSystem.Coffee.Domain.Enums;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.CreateProductCategory;

public class CreateProductCategoryCommandValidator : AbstractValidator<CreateProductCategoryCommand>
{
    public CreateProductCategoryCommandValidator()
    {
        // RuleFor(x => x.BrandId)
        //     .NotEmpty().WithMessage("Không được để trống danh mục!");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã danh mục không được để trống!")
            .MaximumLength(100).WithMessage("Mã danh mục không dài quá 100 kí tự!")
            .MinimumLength(2).WithMessage("Mã danh mục không ít hơn 2 kí tự");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên danh mục không được để trống!")
            .MaximumLength(200).WithMessage("Tên danh mục không được dài quá 200 kí tự!")
            .MinimumLength(2).WithMessage("Tên danh mục phải có ít nhất 2 kí tự");
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Mô tả danh mục không dài quá 1000 kí tự!");
        RuleFor(x => x.DisplayOrder);
        RuleFor(x => x.Status);
    }
}