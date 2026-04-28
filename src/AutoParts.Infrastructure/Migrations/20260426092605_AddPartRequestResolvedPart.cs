using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoParts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartRequestResolvedPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedPartId",
                table: "PartRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartRequests_ResolvedPartId",
                table: "PartRequests",
                column: "ResolvedPartId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartRequests_Parts_ResolvedPartId",
                table: "PartRequests",
                column: "ResolvedPartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartRequests_Parts_ResolvedPartId",
                table: "PartRequests");

            migrationBuilder.DropIndex(
                name: "IX_PartRequests_ResolvedPartId",
                table: "PartRequests");

            migrationBuilder.DropColumn(
                name: "ResolvedPartId",
                table: "PartRequests");
        }
    }
}
