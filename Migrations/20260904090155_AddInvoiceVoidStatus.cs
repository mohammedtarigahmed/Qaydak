using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qaydak.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceVoidStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Invoices");
        }
    }
}
