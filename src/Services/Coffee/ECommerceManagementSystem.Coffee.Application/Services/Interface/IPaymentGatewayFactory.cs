using ECommerceManagementSystem.Coffee.Domain.Entities;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

/// <summary>
/// Factory to resolve payment gateway services by code
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// Get payment gateway service by code
    /// </summary>
    /// <param name="code">Payment method code (e.g., "PAYOS", "PayInCash")</param>
    /// <returns>Payment gateway service instance</returns>
    /// <exception cref="BadHttpRequestException">When gateway code is not supported</exception>
    IPaymentGatewayService GetGateway(string code);
    
    /// <summary>
    /// Get payment gateway service by BrandPaymentMethod entity
    /// Automatically resolves from PaymentMethods.Code
    /// </summary>
    /// <param name="brandPaymentMethod">Brand payment method with included PaymentMethods navigation</param>
    /// <returns>Payment gateway service instance</returns>
    /// <exception cref="BadHttpRequestException">When gateway code is not supported or navigation not loaded</exception>
    IPaymentGatewayService GetGatewayByBrandPaymentMethod(BrandPaymentMethods brandPaymentMethod);
}