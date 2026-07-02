using System;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260702160000_AddCommandeProductionStatut")]
    public partial class AddCommandeProductionStatut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpiration",
                table: "CommandesProduction",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstTerminee",
                table: "CommandesProduction",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateExpiration",
                table: "CommandesProduction");

            migrationBuilder.DropColumn(
                name: "EstTerminee",
                table: "CommandesProduction");
        }
    }
}
