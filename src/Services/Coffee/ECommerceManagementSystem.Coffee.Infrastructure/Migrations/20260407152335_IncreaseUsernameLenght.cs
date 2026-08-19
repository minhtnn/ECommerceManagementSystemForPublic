using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagementSystem.Coffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseUsernameLenght : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Accounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BrandDailySummary",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SummaryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalRevenueGross = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRevenueNet = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrderCount = table.Column<int>(type: "int", nullable: false),
                    TotalRevenueGrossDelivered = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDiscountDelivered = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRevenueNetDelivered = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrderCountDelivered = table.Column<int>(type: "int", nullable: false),
                    TotalQuantitySoldDelivered = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandDailySummary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandDailySummary_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandDailySummary_SummaryDate",
                table: "BrandDailySummary",
                column: "SummaryDate");

            migrationBuilder.CreateIndex(
                name: "UIX_BrandDailySummary_Brand_Date",
                table: "BrandDailySummary",
                columns: new[] { "BrandId", "SummaryDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandDailySummary");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Accounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
