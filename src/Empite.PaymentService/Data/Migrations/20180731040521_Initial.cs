using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.PaymentService.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguredPaymentGateways",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    GatewayName = table.Column<string>(nullable: true),
                    IsEnabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguredPaymentGateways", x => x.Id);
                });

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
                    ExternalContactUserId = table.Column<string>(nullable: true),
                    ExternalPrimaryContactId = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceContacts", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceJobQueues",
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
                    InvoiceGatewayType = table.Column<int>(nullable: false),
                    IsSuccess = table.Column<bool>(nullable: false),
                    ReTryCount = table.Column<int>(nullable: false),
                    LastErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceJobQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
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
                    ItemId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Purcheses",
                columns: table => new
                {
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<Guid>(nullable: true),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<Guid>(nullable: true),
                    Id = table.Column<string>(nullable: false),
                    InvoiceName = table.Column<string>(nullable: true),
                    InvoiceContactUserId = table.Column<string>(nullable: true),
                    ReferenceGuid = table.Column<Guid>(nullable: true),
                    IsPaidForThisMonth = table.Column<bool>(nullable: false),
                    InvoiceType = table.Column<int>(nullable: false),
                    InvoiceStatus = table.Column<int>(nullable: false),
                    InvoiceGatewayType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purcheses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purcheses_InvoiceContacts_InvoiceContactUserId",
                        column: x => x.InvoiceContactUserId,
                        principalTable: "InvoiceContacts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceHistories",
                columns: table => new
                {
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<Guid>(nullable: true),
                    UpdatedAt = table.Column<DateTime>(nullable: true),
                    UpdatedBy = table.Column<Guid>(nullable: true),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<Guid>(nullable: true),
                    Id = table.Column<string>(nullable: false),
                    InvoiceId = table.Column<string>(nullable: true),
                    PurcheseId = table.Column<string>(nullable: true),
                    PaymentRecordedDate = table.Column<DateTime>(nullable: true),
                    InvoiceStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceHistories_Purcheses_PurcheseId",
                        column: x => x.PurcheseId,
                        principalTable: "Purcheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Item_Purchese",
                columns: table => new
                {
                    RecurringInvoiceId = table.Column<string>(nullable: false),
                    ItemId = table.Column<string>(nullable: false),
                    Qty = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item_Purchese", x => new { x.RecurringInvoiceId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_Item_Purchese_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Item_Purchese_Purcheses_RecurringInvoiceId",
                        column: x => x.RecurringInvoiceId,
                        principalTable: "Purcheses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceContacts_ExternalContactUserId",
                table: "InvoiceContacts",
                column: "ExternalContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHistories_InvoiceId",
                table: "InvoiceHistories",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceHistories_PurcheseId",
                table: "InvoiceHistories",
                column: "PurcheseId");

            migrationBuilder.CreateIndex(
                name: "IX_Item_Purchese_ItemId",
                table: "Item_Purchese",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemId",
                table: "Items",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Purcheses_InvoiceContactUserId",
                table: "Purcheses",
                column: "InvoiceContactUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Purcheses_ReferenceGuid",
                table: "Purcheses",
                column: "ReferenceGuid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguredPaymentGateways");

            migrationBuilder.DropTable(
                name: "InvoiceHistories");

            migrationBuilder.DropTable(
                name: "InvoiceJobQueues");

            migrationBuilder.DropTable(
                name: "Item_Purchese");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Purcheses");

            migrationBuilder.DropTable(
                name: "InvoiceContacts");
        }
    }
}
