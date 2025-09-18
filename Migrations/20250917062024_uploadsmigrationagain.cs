using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TanuiApp.Migrations
{
    /// <inheritdoc />
    public partial class uploadsmigrationagain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrlsString",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrlsString",
                table: "Products");
        }
    }
}
