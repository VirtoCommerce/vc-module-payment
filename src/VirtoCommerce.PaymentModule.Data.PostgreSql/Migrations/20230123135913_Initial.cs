using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.PostgreSql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorePaymentMethod",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailableForPartial = table.Column<bool>(type: "boolean", nullable: false),
                    TypeName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StoreId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePaymentMethod", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorePaymentMethodEntity_TypeName_StoreId",
                table: "StorePaymentMethod",
                columns: new[] { "TypeName", "StoreId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorePaymentMethod");
        }
    }
}
