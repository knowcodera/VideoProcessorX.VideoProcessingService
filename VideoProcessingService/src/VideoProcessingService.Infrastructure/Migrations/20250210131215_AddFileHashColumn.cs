using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoProcessingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileHashColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Videos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Videos");
        }
    }
}
