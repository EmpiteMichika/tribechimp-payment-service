using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.PaymentService.Data.Migrations
{
    public partial class addDueDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaidForThisMonth",
                table: "Purcheses");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "InvoiceHistories",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "InvoiceHistories");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaidForThisMonth",
                table: "Purcheses",
                nullable: false,
                defaultValue: false);
        }
    }
}
