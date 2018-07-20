using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.TribechimpService.PaymentService.Data.Migrations
{
    public partial class Addeed_referecnceGuid_index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoices_ReferenceGuid",
                table: "RecurringInvoices",
                column: "ReferenceGuid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecurringInvoices_ReferenceGuid",
                table: "RecurringInvoices");
        }
    }
}
