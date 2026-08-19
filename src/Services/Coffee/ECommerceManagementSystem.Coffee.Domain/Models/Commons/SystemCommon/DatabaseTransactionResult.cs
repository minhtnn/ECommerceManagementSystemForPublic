namespace ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

public class DatabaseTransactionResult
{
    public bool IsSuccess { get; set; }
    public int RowsAffected { get; set; }
    public string Message { get; set; }
    public Exception Exception { get; set; }
    public List<ValidationError> ValidationErrors { get; set; } = new();
}

public class ValidationError
{
    public string[] MemberNames { get; set; }
    public string ErrorMessage { get; set; }
}