using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Query.GetCustomerAddressById;

public class GetCustomerAddressByIdQuery : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string TimeZone {get;set;}
}