using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TanuiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryCompanyId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Town = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    County = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryCompanies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DeliveryCompanyId",
                table: "AspNetUsers",
                column: "DeliveryCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_DeliveryCompanies_DeliveryCompanyId",
                table: "AspNetUsers",
                column: "DeliveryCompanyId",
                principalTable: "DeliveryCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_DeliveryCompanies_DeliveryCompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "DeliveryCompanies");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DeliveryCompanyId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DeliveryCompanyId",
                table: "AspNetUsers");
        }
    }
}
