namespace ECommerceManagementSystem.Coffee.Domain.Constants;

public static class ApiEndpointConstants
{
    public const string RootEndPoint = "/api";
    public const string ApiVersion = "/v1";
    public const string ApiEndpoint = RootEndPoint + ApiVersion;

    public static class Authentication
    {
        public const string Tag = "Authentication";
        public const string AuthenticationEndpoint = ApiEndpoint + "/authentication";
        public const string Login = "/login";
        public const string UpdateInformation = "/update-information";
        public const string CustomerGoogleLoginAndRegister = "customer/google/login-and-register";
        public const string Refresh = "/refresh";
        public const string Create = "/create";
        public const string ChangePassword = "/change-password";
        public const string AccountDetail = "/account-detail";
        public const string CustomerNormalRegister = "/customer-normal-register";
        public const string CustomerGoogleRegister = "/customer-google-register";
        public const string CustomerVerifyEmail = "/customer-verify-email";
        public const string CustomerResendOtpVerifyEmail = "/resend-customer-verify-otp-email";
        public const string ForgotPassword = "/forgot-password";
        public const string ValidateResetToken = "/validate-reset-token";
        public const string ResetPassword = "/reset-password";
        public const string Logout = "/logout";
        public const string LogoutAllDevices = "/logout-all-devices";
    }
    public static class Brand
    {
        public const string Tag = "Brand";
        public const string BrandsEndpoint = ApiEndpoint + "/brands";
        public const string GetBrands = "";
        public const string GetBrandById = "/{id:guid}";
        public const string GetBrandDetails = "/details";
        public const string CreateBrand = "";
        public const string UpdateBrand = "/{id:guid}";
    }
    public static class ProductCategory
    {
        public const string Tag = "ProductCategory";
        public const string ProductCategoriesEndpoint = ApiEndpoint + "/product-categories";
        public const string GetProductCategories = "";
        public const string GetProductCategoryById = "/{id:guid}";
        public const string CreateProductCategory = "";
        public const string UpdateProductCategory = "/{id:guid}";
    }
    public static class Product
    {
        public const string Tag = "Product";
        public const string ProductsEndpoint = ApiEndpoint + "/products";
        public const string GetProducts = "";
        public const string GetProductById = "/{id:guid}";
        public const string GetPublicProductById = "public/{brandCode}/{id:guid}";
        public const string CreateProduct = "";
        public const string UpdateProduct = "/{id:guid}";
    }
    public static class PromotionRule
    {
        public const string Tag = "PromotionRule";
        public const string PromotionRulesEndpoint = ApiEndpoint + "/promotion-rules";
        public const string GetPromotionRules = "";
        public const string GetPromotionRuleById = "/{id:guid}";
        public const string GetPublicPromotionRuleById = "/public/{brandCode}/{id:guid}";
        public const string GetApplicablePromotionRules = "/{brandCode}/applicable";
        public const string CreatePromotionRule = "";
        public const string UpdatePromotionRule = "/{id:guid}";
    }
    public static class Payment
    {
        public const string Tag = "Payment";
        public const string PaymentsEndpoint = ApiEndpoint + "/payments";
        public const string PaymentMethodsEndpoint = ApiEndpoint + "/payment-methods";

        public const string GetPaymentMethods = "";
        public const string GetPaymentMethodById = "/{id:guid}";
        public const string CreatePaymentMethod = "";
        public const string UpdatePaymentMethod = "/{id:guid}";

        public const string GetBrandPaymentMethods = "/brand";
        public const string GetBrandPaymentMethodById = "/brand/{id:guid}";
        public const string CreateBrandPaymentMethod = "/brand";
        public const string UpdateBrandPaymentMethod = "/brand/{id:guid}";

        public const string GetBrandPublicPaymentMethods = "/brand/public/{brandCode}";

        public const string GetPayments = "";
        public const string GetPaymentById = "/{id:guid}";

        public const string CallBackPayOs = "callback/payos";
        public const string ReturnPayOs = "return/payos";
        public const string GetPaymentStatus = "/status/{orderId}";
        public const string CancelPayment = "/cancel/{orderId}";
    }
    public static class Order
    {
        public const string Tag = "Order";
        public const string OrdersEndpoint = ApiEndpoint + "/orders";
        public const string GetOrders = "";
        public const string GetOrderById = "/{id:guid}";
        public const string CreateOrder = "";
        public const string UpdateOrder = "/{id:guid}";

        public const string GetBrandOrders = "/brand";
        public const string GetCustomerOrders = "/customer";
        public const string GetPaymentLink = "/{id}/payment-link";
    }
    public static class Cart
    {
        public const string Tag = "Carts";

        public const string EndCustomerCartsEndpoint = ApiEndpoint + "/carts";

        // public const string GetCarts = "";
        public const string GetCustomerCart = "/end-customer";
        public const string CreateCart = "/end-customer";
        public const string UpdateCart = "/end-customer";
        public const string ApplyPromotion = "/end-customer/apply-promotion";
    }
    public static class Customers
    {
        public const string Tag = "Customers";
        public const string CustomersEndpoint = ApiEndpoint + "/customers";
        public const string CustomerAddressesEndpoint = ApiEndpoint + "/customers/addresses";
        public const string GetCustomers = "";
        public const string GetCustomerById = "/{id:guid}";
        public const string CreateCustomer = "";
        public const string UpdateCustomer = "/{id:guid}";
        public const string GetCustomerAddresses = "/addresses";
        public const string GetCustomerAddressById = "/addresses/{id:guid}";
        public const string CreateCustomerAddress = "/addresses";
        public const string UpdateCustomerAddress = "/addresses/{id:guid}";
        public const string CreateCustomerConsultant = "/consultant/email";
    }
    public static class Menu
    {
        public const string Tag = "Menu";
        public const string MenusEndpoint = ApiEndpoint + "/menus";
        public const string GetMenus = "";
        public const string GetPublicMenus = "/public/{brandCode}";
    }
    public static class Post
    {
        public const string Tag = "Posts";
        public const string PostsEndpoint = ApiEndpoint + "/posts";
        public const string GetBrandPosts = "";
        public const string GetBrandPostById = "/{id:guid}";
        public const string CreateBrandPost = "";
        public const string UpdateBrandPost = "/{id:guid}";
        public const string GetBrandPublicPosts = "/public/{brandCode}";
        public const string GetBrandPublicPostById = "/public/{brandCode}/{id:guid}";
        public const string GetBrandPublicPostOgPreviewById = "/public/{brandCode}/{id:guid}/og";
    }

    public static class SystemConfiguration
    {
        public const string Tag = "SystemConfiguration";
        public const string SystemConfigurationEndpoint = ApiEndpoint + "/system-configurations";
        public const string GetSystemConfigurations = "";
        public const string CreateSystemConfiguration = "";
        public const string UpdateSystemConfiguration = "/{id:guid}";
    }
    
    public static class Statistics
    {
        public const string Tag = "Statistics";
        public const string StatisticsEndpoint = ApiEndpoint + "/statistics";
        public const string GetProductsSaleStatistics = "/products";
        public const string GetPromotionRulesSaleStatistics = "/promotion-rules";
    }
}