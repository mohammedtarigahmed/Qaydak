using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qaydak.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Customers",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Customers");
        }
    }
}
