using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TanuiApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToKenyanLocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "AspNetUsers",
                newName: "Town");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "AspNetUsers",
                newName: "Estate");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "AspNetUsers",
                newName: "County");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Town",
                table: "AspNetUsers",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "Estate",
                table: "AspNetUsers",
                newName: "Country");

            migrationBuilder.RenameColumn(
                name: "County",
                table: "AspNetUsers",
                newName: "City");
        }
    }
}
