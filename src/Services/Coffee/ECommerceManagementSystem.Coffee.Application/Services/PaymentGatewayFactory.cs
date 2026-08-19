using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;

namespace ECommerceManagementSystem.Coffee.Application.Services;

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGatewayService> _gateways;
    private readonly ILogger _logger;

    public PaymentGatewayFactory(
        IEnumerable<IPaymentGatewayService> gateways,
        ILogger logger)
    {
        _gateways = gateways;
        _logger = logger;
    }

    public IPaymentGatewayService GetGateway(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.Error("Payment gateway code cannot be null or empty");
            throw new BadHttpRequestException("Mã phương thức thanh toán không được để trống!");
        }

        var gateway = _gateways.FirstOrDefault(g => 
            g.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        if (gateway == null)
        {
            _logger.Error("Payment gateway not found for code: {Code}", code);
            throw new BadHttpRequestException($"Phương thức thanh toán '{code}' không được hỗ trợ!");
        }

        _logger.Debug("Resolved payment gateway: {Code} -> {GatewayType}", code, gateway.GetType().Name);
        return gateway;
    }

    public IPaymentGatewayService GetGatewayByBrandPaymentMethod(BrandPaymentMethods brandPaymentMethod)
    {
        if (brandPaymentMethod == null)
        {
            _logger.Error("BrandPaymentMethod cannot be null");
            throw new BadHttpRequestException("Thông tin phương thức thanh toán không hợp lệ!");
        }

        if (brandPaymentMethod.PaymentMethods == null)
        {
            _logger.Error(
                "PaymentMethods navigation property not loaded for BrandPaymentMethodId: {Id}",
                brandPaymentMethod.Id);
            throw new BadHttpRequestException(
                "Không thể xác định loại phương thức thanh toán. Vui lòng liên hệ quản trị viên!");
        }

        var code = brandPaymentMethod.PaymentMethods.Code;
        
        _logger.Information(
            "Resolving gateway for BrandPaymentMethod: {BrandPaymentMethodId}, Code: {Code}",
            brandPaymentMethod.Id, code);

        return GetGateway(code);
    }
}