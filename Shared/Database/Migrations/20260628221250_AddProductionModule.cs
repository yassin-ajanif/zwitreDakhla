using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationsProduction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperationAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tables = table.Column<int>(type: "INTEGER", nullable: false),
                    PochetteGrand = table.Column<int>(type: "INTEGER", nullable: false),
                    PochetteMoyenne = table.Column<int>(type: "INTEGER", nullable: false),
                    PochettePetit = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationsProduction", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationsProduction_OperationAt",
                table: "OperationsProduction",
                column: "OperationAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationsProduction");
        }
    }
}
