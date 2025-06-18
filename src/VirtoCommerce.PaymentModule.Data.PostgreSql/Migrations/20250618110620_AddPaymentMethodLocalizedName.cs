using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.PaymentModule.Data.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodLocalizedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethodLocalizedName",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ParentEntityId = table.Column<string>(type: "character varying(128)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodLocalizedName", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethodLocalizedName_StorePaymentMethod_ParentEntityId",
                        column: x => x.ParentEntityId,
                        principalTable: "StorePaymentMethod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodLocalizedName_LanguageCode_ParentEntityId",
                table: "PaymentMethodLocalizedName",
                columns: new[] { "LanguageCode", "ParentEntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodLocalizedName_ParentEntityId",
                table: "PaymentMethodLocalizedName",
                column: "ParentEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethodLocalizedName");
        }
    }
}
