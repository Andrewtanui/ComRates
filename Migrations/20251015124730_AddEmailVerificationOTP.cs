using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TanuiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationOTP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationOTP",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OTPExpiryTime",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationOTP",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OTPExpiryTime",
                table: "AspNetUsers");
        }
    }
}
