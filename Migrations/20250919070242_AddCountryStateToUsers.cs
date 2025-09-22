using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TanuiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryStateToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "County",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Estate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsOnCampus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "University",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Ward",
                table: "AspNetUsers",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "UniversityCounty",
                table: "AspNetUsers",
                newName: "Country");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "AspNetUsers",
                newName: "Ward");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "AspNetUsers",
                newName: "UniversityCounty");

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estate",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnCampus",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "University",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
