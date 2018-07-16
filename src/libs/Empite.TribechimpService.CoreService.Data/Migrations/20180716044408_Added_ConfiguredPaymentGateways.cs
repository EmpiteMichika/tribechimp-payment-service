using Microsoft.EntityFrameworkCore.Migrations;

namespace Empite.TribechimpService.PaymentService.Data.Migrations
{
    public partial class Added_ConfiguredPaymentGateways : Migration
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguredPaymentGateways");
        }
    }
}
