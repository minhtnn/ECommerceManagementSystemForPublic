using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Command.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{

    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
            .MaximumLength(200).WithMessage("Tên sản phẩm không quá 200 ký tự");

        RuleFor(x => x.FullName)
            .MaximumLength(500).WithMessage("Tên đầy đủ không quá 500 ký tự")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không quá 2000 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Giá sản phẩm phải >= 0")
            .When(x => x.Price.HasValue);
        
        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị sản phẩm phải >= 0")
            .When(x => x.DisplayOrder.HasValue);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng tồn kho phải >= 0");

        // ===== VALIDATION CHO IMAGES =====
        
        RuleFor(x => x.NewImageFiles)
            .Must(files => files == null || files.Count <= 4)
            .WithMessage("Chỉ được upload tối đa 4 ảnh mới")
            .When(x => x.NewImageFiles != null);

        RuleForEach(x => x.NewImageFiles)
            .Must(BeValidImageFile)
            .WithMessage("File ảnh không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png, .gif, .webp và kích thước <= 5MB")
            .When(x => x.NewImageFiles != null && x.NewImageFiles.Any());

        // Validate NewImageMetadata phải khớp với số lượng NewImageFiles
        RuleFor(x => x)
            .Must(x => ValidateImageMetadataCount(x.NewImageFiles, x.UpdateNewImageMetadata))
            .WithMessage("Số lượng metadata ảnh phải khớp với số lượng file ảnh mới")
            .When(x => x.NewImageFiles != null && x.NewImageFiles.Any() && x.UpdateNewImageMetadata != null);

        // Validate chỉ có 1 ảnh chính trong ảnh mới
        RuleFor(x => x.UpdateNewImageMetadata)
            .Must(metadata => metadata == null || metadata.Count(m => m.IsMainImage) <= 1)
            .WithMessage("Chỉ được chọn 1 ảnh làm ảnh chính trong các ảnh mới")
            .When(x => x.UpdateNewImageMetadata != null && x.UpdateNewImageMetadata.Any());
        
        RuleFor(x => x.SideAttibutes)
            .Must(attrs => attrs == null || attrs.Count <= 20)
            .WithMessage("Chỉ được thêm tối đa 20 thuộc tính phụ")
            .When(x => x.SideAttibutes != null);

        RuleForEach(x => x.SideAttibutes)
            .ChildRules(attr =>
            {
                attr.RuleFor(a => a.Key)
                    .NotEmpty().WithMessage("Tên thuộc tính không được để trống")
                    .MaximumLength(50).WithMessage("Tên thuộc tính không quá 50 ký tự");

                attr.RuleFor(a => a.Value)
                    .NotEmpty().WithMessage("Giá trị thuộc tính không được để trống")
                    .MaximumLength(200).WithMessage("Giá trị thuộc tính không quá 200 ký tự");
            })
            .When(x => x.SideAttibutes != null && x.SideAttibutes.Any());

        // Validate không có Key trùng lặp
        RuleFor(x => x.SideAttibutes)
            .Must(attrs => attrs == null || attrs.Select(a => a.Key).Distinct().Count() == attrs.Count)
            .WithMessage("Các thuộc tính không được trùng tên")
            .When(x => x.SideAttibutes != null && x.SideAttibutes.Any());

        // ===== BUSINESS RULES =====
        
        // Sản phẩm Active phải có giá
        RuleFor(x => x)
            .Must(x => x.Status != EProductStatus.Active || x.Price.HasValue)
            .WithMessage("Sản phẩm đang hoạt động phải có giá bán");
        
    }
    private bool BeValidImageFile(IFormFile file)
    {
        if (file == null) return false;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return file.Length > 0
               && file.Length <= 5 * 1024 * 1024 // 5MB
               && allowedExtensions.Contains(extension);
    }

    private bool ValidateImageMetadataCount(
        IFormFileCollection? imageFiles, 
        List<UpdateProductImageMetadata>? metadata)
    {
        if (imageFiles == null || !imageFiles.Any()) return true;
        if (metadata == null) return true; // Metadata optional
        
        return imageFiles.Count == metadata.Count;
    }

}