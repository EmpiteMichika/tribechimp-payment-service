using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.PaymentService.Data.Migrations
{
    public partial class addIndexs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Purcheses_InvoiceStatus",
                table: "Purcheses",
                column: "InvoiceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Purcheses_InvoiceType",
                table: "Purcheses",
                column: "InvoiceType");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHistories_InvoiceStatus",
                table: "InvoiceHistories",
                column: "InvoiceStatus");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purcheses_InvoiceStatus",
                table: "Purcheses");

            migrationBuilder.DropIndex(
                name: "IX_Purcheses_InvoiceType",
                table: "Purcheses");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceHistories_InvoiceStatus",
                table: "InvoiceHistories");
        }
    }
}
