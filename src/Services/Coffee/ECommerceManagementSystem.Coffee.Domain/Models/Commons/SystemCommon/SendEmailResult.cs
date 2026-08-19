namespace ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

public class SendEmailResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public string Content  { get; set; }
}