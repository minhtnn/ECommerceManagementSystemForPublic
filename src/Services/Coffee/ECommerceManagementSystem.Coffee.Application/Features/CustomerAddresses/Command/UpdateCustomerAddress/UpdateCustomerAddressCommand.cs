using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.CustomerAddresses.Command.UpdateCustomerAddress;

public class UpdateCustomerAddressCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string Receiver {get; set;}
    public required string Address {get; set;}
    public required string ShippingContact {get; set;}
    public double Latitude {get; set;}
    public double Longitude {get; set;}
    public bool IsPrimary {get; set;}
}