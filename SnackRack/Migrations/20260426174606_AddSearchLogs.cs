using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackRack.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "search_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SearchTerm = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SearchType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LikeResultCount = table.Column<int>(type: "integer", nullable: false),
                    SemanticResultCount = table.Column<int>(type: "integer", nullable: false),
                    TopDistance = table.Column<double>(type: "double precision", nullable: true),
                    Results = table.Column<string>(type: "jsonb", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_search_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_search_logs_SearchedAt",
                table: "search_logs",
                column: "SearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_search_logs_SearchType",
                table: "search_logs",
                column: "SearchType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "search_logs");
        }
    }
}
