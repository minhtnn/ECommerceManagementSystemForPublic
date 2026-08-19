using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Google.Protobuf;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

public interface IMediaService
{
    Task<string> UploadImageAsync(ByteString byteString);
    Task<string> UploadExcelAsync(ByteString byteString);
    Task<string> GetImageUrlAsync(string fileName, TimeSpan? expiration);
    Task<byte[]> DownloadFileAsync(string fileName);
    Task<string> GetImagePermanentUrlAsync(string path);  

    /// <summary>
    /// Upload ảnh từ IFormFile (từ form upload)
    /// </summary>
    /// <param name="file">File upload từ client</param>
    /// <param name="folderPath">Thư mục trên Firebase (VD: "products", "categories")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>MediaUploadResult chứa fileName và trạng thái</returns>
    Task<MediaUploadResult> UploadImageFromFormAsync(
        IFormFile file,
        string folderPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Thay thế tất cả Firebase path trong HTML content thành signed URL.
    /// VD: src="posts/inline/brandId/abc.jpg" → src="https://signed-url..."
    /// </summary>
    Task<string> ResolveContentImageUrlsAsync(
        string htmlContent,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    );
    
    // IMediaService.cs — thêm method:
    /// <summary>
    /// Normalize signed URL trong HTML content về Firebase path trước khi lưu DB.
    /// VD: src="https://storage.googleapis.com/bucket/posts/inline/x.jpg?X-Goog-..."
    ///   → src="posts/inline/x.jpg"
    /// </summary>
    Task<string> NormalizeContentImageUrlsAsync(string? htmlContent);

    /// <summary>
    /// Xóa file trên Firebase Storage
    /// </summary>
    /// <param name="fileName">Tên file cần xóa (bao gồm cả folder path nếu có)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True nếu xóa thành công</returns>
    Task<bool> DeleteFileAsync(
        string fileName,
        CancellationToken cancellationToken = default
    );
}