using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeNaissain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TypesNaissain",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Actif = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesNaissain", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TypesNaissain_Nom",
                table: "TypesNaissain",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypesNaissain_Ordre",
                table: "TypesNaissain",
                column: "Ordre");

            migrationBuilder.Sql("""
                INSERT INTO TypesNaissain (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                VALUES ('Grand', 1, 1, datetime('now'), datetime('now')),
                       ('Moyenne', 1, 2, datetime('now'), datetime('now')),
                       ('Petit', 1, 3, datetime('now'), datetime('now'));
                """);

            migrationBuilder.AddColumn<int>(
                name: "TypeNaissainId",
                table: "CommandesProduction",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE CommandesProduction
                SET TypeNaissainId = (
                    SELECT t.Id FROM TypesNaissain t
                    WHERE t.Nom = CommandesProduction.TypeHuitre
                    LIMIT 1)
                WHERE EXISTS (
                    SELECT 1 FROM TypesNaissain t
                    WHERE t.Nom = CommandesProduction.TypeHuitre);
                """);

            migrationBuilder.DropColumn(
                name: "TypeHuitre",
                table: "CommandesProduction");

            migrationBuilder.CreateIndex(
                name: "IX_CommandesProduction_TypeNaissainId",
                table: "CommandesProduction",
                column: "TypeNaissainId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandesProduction_TypesNaissain_TypeNaissainId",
                table: "CommandesProduction",
                column: "TypeNaissainId",
                principalTable: "TypesNaissain",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandesProduction_TypesNaissain_TypeNaissainId",
                table: "CommandesProduction");

            migrationBuilder.DropTable(
                name: "TypesNaissain");

            migrationBuilder.DropIndex(
                name: "IX_CommandesProduction_TypeNaissainId",
                table: "CommandesProduction");

            migrationBuilder.DropColumn(
                name: "TypeNaissainId",
                table: "CommandesProduction");

            migrationBuilder.AddColumn<string>(
                name: "TypeHuitre",
                table: "CommandesProduction",
                type: "TEXT",
                nullable: false,
                defaultValue: "Grand");
        }
    }
}
