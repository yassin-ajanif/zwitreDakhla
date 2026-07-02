using System;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260702133000_AddCategorieCommande")]
    public partial class AddCategorieCommande : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriesCommande",
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
                    table.PrimaryKey("PK_CategoriesCommande", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesCommande_Nom",
                table: "CategoriesCommande",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesCommande_Ordre",
                table: "CategoriesCommande",
                column: "Ordre");

            migrationBuilder.Sql("""
                INSERT INTO CategoriesCommande (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                VALUES ('Catégorie A', 1, 1, datetime('now'), datetime('now')),
                       ('Catégorie B', 1, 2, datetime('now'), datetime('now'));
                """);

            migrationBuilder.AddColumn<int>(
                name: "CategorieCommandeId",
                table: "CommandesProduction",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_CommandesProduction_CategorieCommandeId",
                table: "CommandesProduction",
                column: "CategorieCommandeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoriesCommande");

            migrationBuilder.DropIndex(
                name: "IX_CommandesProduction_CategorieCommandeId",
                table: "CommandesProduction");

            migrationBuilder.DropColumn(
                name: "CategorieCommandeId",
                table: "CommandesProduction");
        }
    }
}
