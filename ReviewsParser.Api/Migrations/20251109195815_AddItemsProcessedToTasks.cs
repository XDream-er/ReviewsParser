using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewsParser.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemsProcessedToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemsProcessed",
                table: "ParsingTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemsProcessed",
                table: "ParsingTasks");
        }
    }
}
