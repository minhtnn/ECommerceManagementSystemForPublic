namespace ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;

public enum ECacheInvalidationType
{
    /// <summary>
    /// Xóa ngay lập tức
    /// </summary>
    Immediate,
    
    /// <summary>
    /// Đang đếm, chưa đạt threshold
    /// </summary>
    Debounced,
    
    /// <summary>
    /// Đạt threshold - đã xóa cache
    /// </summary>
    ThresholdReached,
    
    /// <summary>
    /// Không có keys nào để xóa
    /// </summary>
    NoKeys,
    
    /// <summary>
    /// Có process khác đang xóa cache
    /// </summary>
    LockSkipped,
    
    /// <summary>
    /// Có lỗi xảy ra
    /// </summary>
    Failed
}