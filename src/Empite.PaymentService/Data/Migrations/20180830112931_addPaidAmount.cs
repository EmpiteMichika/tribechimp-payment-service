using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.PaymentService.Data.Migrations
{
    public partial class addPaidAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PaidAmount",
                table: "InvoiceHistories",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "InvoiceHistories");
        }
    }
}
