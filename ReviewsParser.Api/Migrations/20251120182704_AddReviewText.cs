using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewsParser.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewText",
                table: "ParsedReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewText",
                table: "ParsedReviews");
        }
    }
}
