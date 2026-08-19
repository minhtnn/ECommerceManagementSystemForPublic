using FluentValidation;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.CreateBrandPost;

public class CreateBrandPostCommandValidator : AbstractValidator<CreateBrandPostCommand>
{
    public CreateBrandPostCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã bài đăng không được để trống!")
            .MaximumLength(100).WithMessage("Mã bài đăng không được dài quá 100 ký tự!")
            .MinimumLength(2).WithMessage("Mã bài đăng không được ít hơn 2 ký tự!");

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
            .MaximumLength(1000).WithMessage("Tóm tắt không được dài quá 1000 ký tự!")
            .When(x => !string.IsNullOrEmpty(x.Excerpt));
        
        RuleFor(x => x.Image)
            .Must(img => img == null || img.Length <= 5 * 1024 * 1024)
            .WithMessage("Featured image không được vượt quá 5MB!")
            .Must(img => img == null || AllowedImageTypes.Contains(img.ContentType.ToLower()))
            .WithMessage("Featured image chỉ chấp nhận .jpg, .jpeg, .png, .gif, .webp!")
            .When(x => x.Image != null);

        RuleFor(x => x.InlineImages)
            .Must(images => images == null || images.All(img =>
                img.Length <= 5 * 1024 * 1024))
            .WithMessage("Mỗi ảnh inline không được vượt quá 5MB!")
            .Must(images => images == null || images.All(img =>
                AllowedImageTypes.Contains(img.ContentType.ToLower())))
            .WithMessage("Ảnh inline chỉ chấp nhận .jpg, .jpeg, .png, .gif, .webp!")
            .When(x => x.InlineImages != null && x.InlineImages.Count > 0);
    }
    
    private static readonly string[] AllowedImageTypes =
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
    };

}