To migration:
    - Add: dotnet ef migrations add IncreaseUsernameLenght -p ECommerceManagementSystem.Coffee.Infrastructure -s ECommerceManagementSystem.Coffee.Application
Database:
    - Drop: dotnet ef database drop -p ECommerceManagementSystem.Coffee.Infrastructure -s ECommerceManagementSystem.Coffee.Application --force
    - Create: dotnet ef database update -p ECommerceManagementSystem.Coffee.Infrastructure -s ECommerceManagementSystem.Coffee.Application