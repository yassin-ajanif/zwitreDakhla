using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandeProductionToBonReception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommandeProductionId",
                table: "BonsReception",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsReception_CommandesProduction_CommandeProductionId",
                table: "BonsReception",
                column: "CommandeProductionId",
                principalTable: "CommandesProduction",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsReception_CommandesProduction_CommandeProductionId",
                table: "BonsReception");

            migrationBuilder.DropColumn(
                name: "CommandeProductionId",
                table: "BonsReception");
        }
    }
}
