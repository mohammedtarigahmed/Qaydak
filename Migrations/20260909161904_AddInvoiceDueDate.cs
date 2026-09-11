using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qaydak.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");
        }
    }
}
