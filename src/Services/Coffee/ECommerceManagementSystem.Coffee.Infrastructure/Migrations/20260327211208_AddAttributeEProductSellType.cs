using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagementSystem.Coffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeEProductSellType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductSellType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "ProductSell");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductSellType",
                table: "Products");
        }
    }
}
