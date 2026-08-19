namespace ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

public class MediaUploadResult
{
    public bool IsSuccess { get; set; }
    public string? FileName { get; set; }
    public string? Message { get; set; }
    public Exception? Exception { get; set; }
    
    public static MediaUploadResult Success(string fileName)
    {
        return new MediaUploadResult
        {
            IsSuccess = true,
            FileName = fileName,
            Message = "Upload thành công"
        };
    }
    
    public static MediaUploadResult Failure(string message, Exception? ex = null)
    {
        return new MediaUploadResult
        {
            IsSuccess = false,
            Message = message,
            Exception = ex
        };
    }
}