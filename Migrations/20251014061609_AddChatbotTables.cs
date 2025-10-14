using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TanuiApp.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BotResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedIntent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    WasHelpful = table.Column<bool>(type: "bit", nullable: false),
                    UserFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatbotConversations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotTrainingData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AddedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotTrainingData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotConversations_SessionId",
                table: "ChatbotConversations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotConversations_UserId_CreatedAt",
                table: "ChatbotConversations",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotTrainingData_Intent",
                table: "ChatbotTrainingData",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotTrainingData_IsActive",
                table: "ChatbotTrainingData",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotConversations");

            migrationBuilder.DropTable(
                name: "ChatbotTrainingData");
        }
    }
}
