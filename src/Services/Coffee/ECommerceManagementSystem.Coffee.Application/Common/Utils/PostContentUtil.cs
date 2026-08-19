// Application/Common/Utils/PostContentUtil.cs
using System.Text.RegularExpressions;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;

namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

public static class PostContentUtil
{
    /// <summary>
    /// Upload inline images, replace placeholder src="__inline_0__"
    /// bằng Firebase path. KHÔNG lưu signed URL vào DB.
    /// </summary>
    public static async Task<(string ResolvedHtml, List<string> UploadedFileNames)>
        UploadInlineImagesAsync(
            string? htmlContent,
            IFormFileCollection? inlineImages,
            IMediaService mediaService,
            Guid brandId,
            CancellationToken cancellationToken = default)
    {
        var uploadedFileNames = new List<string>();

        if (string.IsNullOrEmpty(htmlContent)
            || inlineImages == null
            || inlineImages.Count == 0)
            return (htmlContent ?? string.Empty, uploadedFileNames);

        var result = htmlContent;

        for (int i = 0; i < inlineImages.Count; i++)
        {
            var placeholder = $"__inline_{i}__";
            if (!result.Contains($"src=\"{placeholder}\""))
                continue;

            if (!ImageUtil.IsValidImageFile(inlineImages[i]))
                throw new BadHttpRequestException(
                    $"Ảnh inline [{i}] không hợp lệ. " +
                    "Chỉ chấp nhận .jpg, .jpeg, .png, .gif, .webp và <= 5MB"
                );

            var uploadResult = await mediaService.UploadImageFromFormAsync(
                inlineImages[i],
                folderPath: $"posts/inline/{brandId}",
                cancellationToken
            );

            if (!uploadResult.IsSuccess || string.IsNullOrEmpty(uploadResult.FileName))
                throw new Exception(
                    $"Không thể upload ảnh inline [{i}]: {uploadResult.Message}"
                );

            uploadedFileNames.Add(uploadResult.FileName);
            result = result.Replace(
                $"src=\"{placeholder}\"",
                $"src=\"{uploadResult.FileName}\""
            );
        }

        return (result, uploadedFileNames);
    }

    /// <summary>
    /// Extract tất cả Firebase path của ảnh inline từ HTML.
    /// Dùng để xác định ảnh cũ cần xóa sau khi update.
    /// </summary>
    public static List<string> ExtractInlineImagePaths(string? htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent)) return new List<string>();

        var pattern = @"src=""(posts/inline/[^""]+)""";
        return Regex.Matches(htmlContent, pattern)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Rollback: xóa ảnh đã upload nếu có lỗi.
    /// </summary>
    public static async Task RollbackInlineImagesAsync(
        List<string> fileNames,
        IMediaService mediaService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var fileName in fileNames)
        {
            try
            {
                await mediaService.DeleteFileAsync(fileName, cancellationToken);
                logger.Information("Rollback: deleted {FileName}", fileName);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Rollback: failed to delete {FileName}", fileName);
            }
        }
    }
}