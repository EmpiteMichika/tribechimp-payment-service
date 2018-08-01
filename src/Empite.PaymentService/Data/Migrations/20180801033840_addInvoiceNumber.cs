using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.PaymentService.Data.Migrations
{
    public partial class addInvoiceNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "InvoiceHistories",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purcheses_InvoiceGatewayType",
                table: "Purcheses",
                column: "InvoiceGatewayType");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceJobQueues_InvoiceGatewayType",
                table: "InvoiceJobQueues",
                column: "InvoiceGatewayType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purcheses_InvoiceGatewayType",
                table: "Purcheses");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceJobQueues_InvoiceGatewayType",
                table: "InvoiceJobQueues");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "InvoiceHistories");
        }
    }
}
