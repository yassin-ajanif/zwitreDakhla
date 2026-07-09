using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveBonReceptionLinkToCommandeProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BonReceptionId",
                table: "CommandesProduction",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE CommandesProduction
                SET BonReceptionId = (
                    SELECT b.Id
                    FROM BonsReception b
                    WHERE b.CommandeProductionId = CommandesProduction.Id
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM BonsReception b
                    WHERE b.CommandeProductionId = CommandesProduction.Id
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO BonsReception (
                    Numero,
                    FournisseurId,
                    Date,
                    Note,
                    TotalTtc,
                    CreatedAt,
                    UpdatedAt,
                    CreatedByUserId)
                SELECT
                    'BR-MIG-' || printf('%06d', c.Id),
                    c.FournisseurId,
                    c.DateCommande,
                    COALESCE(c.Note, ''),
                    0,
                    COALESCE(c.CreatedAt, datetime('now')),
                    COALESCE(c.UpdatedAt, datetime('now')),
                    c.CreatedByUserId
                FROM CommandesProduction c
                WHERE c.BonReceptionId IS NULL;

                UPDATE CommandesProduction
                SET BonReceptionId = (
                    SELECT b.Id
                    FROM BonsReception b
                    WHERE b.Numero = 'BR-MIG-' || printf('%06d', CommandesProduction.Id)
                    LIMIT 1
                )
                WHERE BonReceptionId IS NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_BonsReception_CommandesProduction_CommandeProductionId",
                table: "BonsReception");

            migrationBuilder.DropIndex(
                name: "IX_BonsReception_CommandeProductionId",
                table: "BonsReception");

            migrationBuilder.DropColumn(
                name: "CommandeProductionId",
                table: "BonsReception");

            migrationBuilder.AlterColumn<int>(
                name: "BonReceptionId",
                table: "CommandesProduction",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int?),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandesProduction_BonReceptionId",
                table: "CommandesProduction",
                column: "BonReceptionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandesProduction_BonsReception_BonReceptionId",
                table: "CommandesProduction",
                column: "BonReceptionId",
                principalTable: "BonsReception",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandesProduction_BonsReception_BonReceptionId",
                table: "CommandesProduction");

            migrationBuilder.DropIndex(
                name: "IX_CommandesProduction_BonReceptionId",
                table: "CommandesProduction");

            migrationBuilder.AlterColumn<int>(
                name: "BonReceptionId",
                table: "CommandesProduction",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "CommandeProductionId",
                table: "BonsReception",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE BonsReception
                SET CommandeProductionId = (
                    SELECT c.Id
                    FROM CommandesProduction c
                    WHERE c.BonReceptionId = BonsReception.Id
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM CommandesProduction c
                    WHERE c.BonReceptionId = BonsReception.Id
                );
                """);

            migrationBuilder.DropColumn(
                name: "BonReceptionId",
                table: "CommandesProduction");

            migrationBuilder.CreateIndex(
                name: "IX_BonsReception_CommandeProductionId",
                table: "BonsReception",
                column: "CommandeProductionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BonsReception_CommandesProduction_CommandeProductionId",
                table: "BonsReception",
                column: "CommandeProductionId",
                principalTable: "CommandesProduction",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
