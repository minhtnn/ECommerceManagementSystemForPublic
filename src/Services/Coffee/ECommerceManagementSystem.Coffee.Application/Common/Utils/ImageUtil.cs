namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

public static class ImageUtil
{
    
    private static readonly string[] AllowedImageExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    /// <summary>
    /// Validate file ảnh
    /// </summary>
    public static bool IsValidImageFile(IFormFile? file)
    {
        if (file == null)
        {
            return true;
        }
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return file.Length > 0
               && file.Length <= 5 * 1024 * 1024
               && AllowedImageExtensions.Contains(extension);
    }
}