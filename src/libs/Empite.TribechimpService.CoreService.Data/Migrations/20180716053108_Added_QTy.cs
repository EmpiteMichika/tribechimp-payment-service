using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.TribechimpService.PaymentService.Data.Migrations
{
    public partial class Added_QTy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Qty",
                table: "ZohoItemRecurringInvoice",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Qty",
                table: "ZohoItemRecurringInvoice");
        }
    }
}
