using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionQualitySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgrandissementMaxJours",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 365);

            migrationBuilder.AddColumn<int>(
                name: "ImportanceTauxAgrandissement",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "ImportanceTauxMortalite",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgrandissementMaxJours",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ImportanceTauxAgrandissement",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "ImportanceTauxMortalite",
                table: "AppSettings");
        }
    }
}
