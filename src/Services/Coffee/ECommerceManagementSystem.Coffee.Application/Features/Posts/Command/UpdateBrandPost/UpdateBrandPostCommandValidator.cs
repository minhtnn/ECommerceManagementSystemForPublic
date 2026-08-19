using ECommerceManagementSystem.Coffee.Domain.Enums;
using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.UpdateBrandPost;

public class UpdateBrandPostCommandValidator : AbstractValidator<UpdateBrandPostCommand>
{
    public UpdateBrandPostCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID bài đăng không được để trống!");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề bài đăng không được để trống!")
            .MaximumLength(500).WithMessage("Tiêu đề bài đăng không được dài quá 500 ký tự!")
            .MinimumLength(2).WithMessage("Tiêu đề bài đăng không được ít hơn 2 ký tự!");

        RuleFor(x => x.Slug)
            .MaximumLength(500).WithMessage("Slug không được dài quá 500 ký tự!")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug chỉ được chứa chữ thường, số và dấu gạch ngang!")
            .When(x => !string.IsNullOrEmpty(x.Slug));

        RuleFor(x => x.Excerpt)
            .MaximumLength(1000).WithMessage("Tóm tắt bài đăng không được dài quá 1000 ký tự!")
            .When(x => !string.IsNullOrEmpty(x.Excerpt));

        // RuleFor(x => x.Content)
        //     .MaximumLength(50000).WithMessage("Nội dung bài đăng không được dài quá 50000 ký tự!")
        //     .When(x => !string.IsNullOrEmpty(x.Content));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Trạng thái bài đăng không hợp lệ!");

        RuleFor(x => x.Image)
            .Must(image => image == null || image.Length <= 5 * 1024 * 1024)
            .WithMessage("Hình ảnh không được vượt quá 5MB!")
            .Must(image => image == null || new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" }
                .Contains(image.ContentType.ToLower()))
            .WithMessage("Hình ảnh chỉ chấp nhận định dạng .jpg, .jpeg, .png, .gif, .webp!")
            .When(x => x.Image != null);
    }
}