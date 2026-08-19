using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Customers.Command.SendCustomerConsult;

public class SendCustomerConsultCommand : IRequest<ApiResponse>
{
    public required string CustomerFullName { get; set; }
    public required string CustomerEmail { get; set; }
    public required string CustomerPhone { get; set; }
    public required string CustomerMessage { get; set; }
    public required string BrandCode { get; set; }
}