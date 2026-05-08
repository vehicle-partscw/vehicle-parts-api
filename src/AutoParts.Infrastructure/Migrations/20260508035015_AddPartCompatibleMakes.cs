using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoParts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartCompatibleMakes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompatibleMakes",
                table: "Parts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompatibleMakes",
                table: "Parts");
        }
    }
}
