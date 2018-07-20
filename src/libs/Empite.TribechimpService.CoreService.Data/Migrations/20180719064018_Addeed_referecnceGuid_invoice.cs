using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.TribechimpService.PaymentService.Data.Migrations
{
    public partial class Addeed_referecnceGuid_invoice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceGuid",
                table: "RecurringInvoices",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceGuid",
                table: "RecurringInvoices");
        }
    }
}
