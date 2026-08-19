using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerceManagementSystem.Coffee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemConfigurationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConfigKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DependentKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemConfigDependencies_SystemConfigKeys_DependentKeyId",
                        column: x => x.DependentKeyId,
                        principalTable: "SystemConfigKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemConfigDependencies_SystemConfigKeys_TriggerKeyId",
                        column: x => x.TriggerKeyId,
                        principalTable: "SystemConfigKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemConfigValues_SystemConfigKeys_ConfigKeyId",
                        column: x => x.ConfigKeyId,
                        principalTable: "SystemConfigKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_SystemConfigDependencies_Unique",
                table: "SystemConfigDependencies",
                columns: new[] { "TriggerKeyId", "TriggerValue", "DependentKeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigDependencies_DependentKeyId",
                table: "SystemConfigDependencies",
                column: "DependentKeyId");

            migrationBuilder.CreateIndex(
                name: "IDX_SystemConfigKeys_Key",
                table: "SystemConfigKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_SystemConfigValues_ConfigKeyId",
                table: "SystemConfigValues",
                column: "ConfigKeyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemConfigDependencies");

            migrationBuilder.DropTable(
                name: "SystemConfigValues");

            migrationBuilder.DropTable(
                name: "SystemConfigKeys");
        }
    }
}
