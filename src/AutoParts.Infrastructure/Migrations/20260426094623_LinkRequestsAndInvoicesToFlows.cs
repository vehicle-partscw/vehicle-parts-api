using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoParts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkRequestsAndInvoicesToFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedAppointmentId",
                table: "SalesInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_RelatedAppointmentId",
                table: "SalesInvoices",
                column: "RelatedAppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_RelatedAppointmentId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "RelatedAppointmentId",
                table: "SalesInvoices");
        }
    }
}
