using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagementSystem.Coffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_PasswordReset_Complete_Audit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAddresses_Customers_CustomerId",
                table: "CustomerAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailNotifications_Orders_OrderId",
                table: "EmailNotifications");

            migrationBuilder.DropTable(
                name: "ProductSideAttibutes");

            migrationBuilder.AddColumn<int>(
                name: "PasswordChangedCount",
                table: "Accounts",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordLastChangedAt",
                table: "Accounts",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PasswordResetFailedAttempts",
                table: "Accounts",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetLockedUntil",
                table: "Accounts",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Accounts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "Accounts",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PasswordResetTokenUsed",
                table: "Accounts",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenUsedAt",
                table: "Accounts",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasswordResetAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartialToken = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "NVARCHAR(MAX)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetAuditLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSideAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSideAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSideAttributes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_PasswordResetLockedUntil",
                table: "Accounts",
                column: "PasswordResetLockedUntil",
                filter: "[PasswordResetLockedUntil] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_PasswordResetToken",
                table: "Accounts",
                column: "PasswordResetToken",
                filter: "[PasswordResetToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_PasswordResetTokenExpiry",
                table: "Accounts",
                column: "PasswordResetTokenExpiry",
                filter: "[PasswordResetTokenExpiry] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetAuditLogs_AccountId_CreatedDate",
                table: "PasswordResetAuditLogs",
                columns: new[] { "AccountId", "CreatedDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetAuditLogs_Action",
                table: "PasswordResetAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetAuditLogs_CreatedDate",
                table: "PasswordResetAuditLogs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetAuditLogs_IpAddress",
                table: "PasswordResetAuditLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetAuditLogs_Success",
                table: "PasswordResetAuditLogs",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSideAttributes_ProductId",
                table: "ProductSideAttributes",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAddresses_Customers_CustomerId",
                table: "CustomerAddresses",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailNotifications_Orders_OrderId",
                table: "EmailNotifications",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerAddresses_Customers_CustomerId",
                table: "CustomerAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailNotifications_Orders_OrderId",
                table: "EmailNotifications");

            migrationBuilder.DropTable(
                name: "PasswordResetAuditLogs");

            migrationBuilder.DropTable(
                name: "ProductSideAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_PasswordResetLockedUntil",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_PasswordResetToken",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_PasswordResetTokenExpiry",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordChangedCount",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordLastChangedAt",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetFailedAttempts",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetLockedUntil",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenUsed",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenUsedAt",
                table: "Accounts");

            migrationBuilder.CreateTable(
                name: "ProductSideAttibutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSideAttibutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSideAttibutes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSideAttibutes_ProductId",
                table: "ProductSideAttibutes",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerAddresses_Customers_CustomerId",
                table: "CustomerAddresses",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmailNotifications_Orders_OrderId",
                table: "EmailNotifications",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
