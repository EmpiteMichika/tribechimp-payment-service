using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.TribechimpService.PaymentService.Data.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceContacts",
                columns: table => new
                {
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<Guid>(nullable: true),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<Guid>(nullable: true),
                    OrganizationId = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false),
                    ZohoContactUserId = table.Column<string>(nullable: true),
                    ZohoPrimaryContactId = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceContacts", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ZohoInvoiceJobQueues",
                columns: table => new
                {
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<Guid>(nullable: true),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<Guid>(nullable: true),
                    Id = table.Column<string>(nullable: false),
                    JsonData = table.Column<string>(type: "longtext", nullable: true),
                    JobType = table.Column<int>(nullable: false),
                    IsSuccess = table.Column<bool>(nullable: false),
                    ReTryCount = table.Column<int>(nullable: false),
                    LastErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZohoInvoiceJobQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZohoItems",
                columns: table => new
                {
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<Guid>(nullable: true),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<Guid>(nullable: true),
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Rate = table.Column<double>(nullable: false),
                    ZohoItemId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZohoItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurringInvoices",
                columns: table => new
                {
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<Guid>(nullable: true),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<Guid>(nullable: true),
                    Id = table.Column<string>(nullable: false),
                    RecurringInvoiceId = table.Column<string>(nullable: true),
                    InvoiceContactUserId = table.Column<string>(nullable: true),
                    IsDue = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringInvoices_InvoiceContacts_InvoiceContactUserId",
                        column: x => x.InvoiceContactUserId,
                        principalTable: "InvoiceContacts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZohoItemRecurringInvoice",
                columns: table => new
                {
                    RecurringInvoiceId = table.Column<string>(nullable: false),
                    ZohoItemId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZohoItemRecurringInvoice", x => new { x.RecurringInvoiceId, x.ZohoItemId });
                    table.ForeignKey(
                        name: "FK_ZohoItemRecurringInvoice_RecurringInvoices_RecurringInvoiceId",
                        column: x => x.RecurringInvoiceId,
                        principalTable: "RecurringInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZohoItemRecurringInvoice_ZohoItems_ZohoItemId",
                        column: x => x.ZohoItemId,
                        principalTable: "ZohoItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceContacts_ZohoContactUserId",
                table: "InvoiceContacts",
                column: "ZohoContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringInvoices_InvoiceContactUserId",
                table: "RecurringInvoices",
                column: "InvoiceContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZohoItemRecurringInvoice_ZohoItemId",
                table: "ZohoItemRecurringInvoice",
                column: "ZohoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ZohoItems_ZohoItemId",
                table: "ZohoItems",
                column: "ZohoItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZohoInvoiceJobQueues");

            migrationBuilder.DropTable(
                name: "ZohoItemRecurringInvoice");

            migrationBuilder.DropTable(
                name: "RecurringInvoices");

            migrationBuilder.DropTable(
                name: "ZohoItems");

            migrationBuilder.DropTable(
                name: "InvoiceContacts");
        }
    }
}
