using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandeProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandesProduction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    FournisseurId = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeHuitre = table.Column<string>(type: "TEXT", nullable: false),
                    QuantiteNaissain = table.Column<int>(type: "INTEGER", nullable: false),
                    TauxMortalite = table.Column<decimal>(type: "TEXT", nullable: false),
                    DateCommande = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandesProduction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandesProduction_Tiers_FournisseurId",
                        column: x => x.FournisseurId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandesProduction_FournisseurId",
                table: "CommandesProduction",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandesProduction_DateCommande",
                table: "CommandesProduction",
                column: "DateCommande");

            migrationBuilder.AddColumn<int>(
                name: "CommandeProductionId",
                table: "OperationsProduction",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationsProduction_CommandeProductionId",
                table: "OperationsProduction",
                column: "CommandeProductionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationsProduction_CommandesProduction_CommandeProductionId",
                table: "OperationsProduction",
                column: "CommandeProductionId",
                principalTable: "CommandesProduction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                INSERT INTO CommandesProduction (
                    Numero, FournisseurId, TypeHuitre, QuantiteNaissain, TauxMortalite,
                    DateCommande, Note, CreatedAt, UpdatedAt)
                SELECT
                    'CMD-MIG-' || o.Id,
                    COALESCE(
                        (SELECT Id FROM Tiers WHERE Type IN (1, 2) ORDER BY Id LIMIT 1),
                        (SELECT Id FROM Tiers ORDER BY Id LIMIT 1)),
                    'Grand',
                    0,
                    0,
                    date(o.OperationAt),
                    'Migration',
                    o.CreatedAt,
                    o.UpdatedAt
                FROM OperationsProduction o
                WHERE o.CommandeProductionId IS NULL
                  AND COALESCE(
                        (SELECT Id FROM Tiers WHERE Type IN (1, 2) ORDER BY Id LIMIT 1),
                        (SELECT Id FROM Tiers ORDER BY Id LIMIT 1)) IS NOT NULL;

                UPDATE OperationsProduction
                SET CommandeProductionId = (
                    SELECT c.Id FROM CommandesProduction c
                    WHERE c.Numero = 'CMD-MIG-' || OperationsProduction.Id)
                WHERE CommandeProductionId IS NULL
                  AND EXISTS (
                    SELECT 1 FROM CommandesProduction c
                    WHERE c.Numero = 'CMD-MIG-' || OperationsProduction.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationsProduction_CommandesProduction_CommandeProductionId",
                table: "OperationsProduction");

            migrationBuilder.DropTable(
                name: "CommandesProduction");

            migrationBuilder.DropIndex(
                name: "IX_OperationsProduction_CommandeProductionId",
                table: "OperationsProduction");

            migrationBuilder.DropColumn(
                name: "CommandeProductionId",
                table: "OperationsProduction");
        }
    }
}
