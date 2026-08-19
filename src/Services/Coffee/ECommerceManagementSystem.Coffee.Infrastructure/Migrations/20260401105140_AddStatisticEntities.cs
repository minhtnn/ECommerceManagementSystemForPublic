using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagementSystem.Coffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AggregatedAt",
                table: "Orders",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAggregated",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DailyProductSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SaleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalQuantitySold = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalGiftQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalRevenueGross = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrderCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyProductSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyProductSales_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyPromotionStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalDiscountIssued = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrdersUsed = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalRevenueWithPromo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPromotionStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPromotionStats_PromotionRules_PromotionRuleId",
                        column: x => x.PromotionRuleId,
                        principalTable: "PromotionRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_IsAggregated",
                table: "Orders",
                columns: new[] { "OrderStatus", "IsAggregated" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyProductSales_SaleDate",
                table: "DailyProductSales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "UIX_DailyProductSales_Product_Date",
                table: "DailyProductSales",
                columns: new[] { "ProductId", "SaleDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPromotionStats_StatDate",
                table: "DailyPromotionStats",
                column: "StatDate");

            migrationBuilder.CreateIndex(
                name: "UIX_DailyPromotionStats_Promotion_Date",
                table: "DailyPromotionStats",
                columns: new[] { "PromotionRuleId", "StatDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyProductSales");

            migrationBuilder.DropTable(
                name: "DailyPromotionStats");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_IsAggregated",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AggregatedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsAggregated",
                table: "Orders");
        }
    }
}
