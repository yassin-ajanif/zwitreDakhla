using System;
using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandeProductionIdToOperationsProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
            connection.Open();

            if (!TableExists(connection, "OperationsProduction"))
                return;

            var hasCommandeProductionId = ColumnExists(connection, "OperationsProduction", "CommandeProductionId");

            migrationBuilder.Sql("PRAGMA foreign_keys=OFF;", suppressTransaction: true);

            if (!hasCommandeProductionId)
            {
                migrationBuilder.Sql(
                    "ALTER TABLE OperationsProduction ADD COLUMN CommandeProductionId INTEGER NULL;");
            }

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS IX_OperationsProduction_CommandeProductionId
                ON OperationsProduction (CommandeProductionId);
                """);

            if (TableExists(connection, "CommandesProduction")
                && !ForeignKeyExists(connection, "FK_OperationsProduction_CommandesProduction_CommandeProductionId"))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_OperationsProduction_CommandesProduction_CommandeProductionId",
                    table: "OperationsProduction",
                    column: "CommandeProductionId",
                    principalTable: "CommandesProduction",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            }

            migrationBuilder.Sql("PRAGMA foreign_keys=ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
            connection.Open();

            if (!TableExists(connection, "OperationsProduction"))
                return;

            migrationBuilder.Sql("PRAGMA foreign_keys=OFF;", suppressTransaction: true);

            DropForeignKeyIfExists(
                connection,
                migrationBuilder,
                "FK_OperationsProduction_CommandesProduction_CommandeProductionId");

            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_OperationsProduction_CommandeProductionId;");

            if (ColumnExists(connection, "OperationsProduction", "CommandeProductionId"))
            {
                migrationBuilder.Sql("ALTER TABLE OperationsProduction DROP COLUMN CommandeProductionId;");
            }

            migrationBuilder.Sql("PRAGMA foreign_keys=ON;", suppressTransaction: true);
        }

        private static bool TableExists(SqliteConnection connection, string tableName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
            cmd.Parameters.AddWithValue("$name", tableName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = $name;";
            cmd.Parameters.AddWithValue("$name", columnName);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void DropForeignKeyIfExists(
            SqliteConnection connection,
            MigrationBuilder migrationBuilder,
            string foreignKeyName)
        {
            if (!ForeignKeyExists(connection, foreignKeyName))
                return;

            migrationBuilder.DropForeignKey(foreignKeyName, "OperationsProduction");
        }

        private static bool ForeignKeyExists(SqliteConnection connection, string foreignKeyName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = 'OperationsProduction'
                  AND sql LIKE $pattern;
                """;
            cmd.Parameters.AddWithValue("$pattern", $"%{foreignKeyName}%");
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
