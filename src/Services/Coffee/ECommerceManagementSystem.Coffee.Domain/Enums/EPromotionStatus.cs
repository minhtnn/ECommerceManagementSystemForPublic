namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum EPromotionStatus
{
    Draft = 0,      // Vừa được Duplicate, chưa set ngày, chưa active
    Pending = 1,      // Vừa được Duplicate, chưa set ngày, chưa active
    Active = 2,     // Đang hoạt động bình thường
    Inactive = 3,   // Bị tắt thủ công (deactivate khẩn cấp)
    Expired = 4     // Đã hết hạn tự nhiên (now > endDate)
}