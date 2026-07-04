using System;
using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameTypeNaissainToTypeHuitre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
            connection.Open();

            var hasTypesNaissain = TableExists(connection, "TypesNaissain");
            var hasTypesHuitre = TableExists(connection, "TypesHuitre");
            var hasTypeText = ColumnExists(connection, "CommandesProduction", "TypeHuitre");
            var hasTypeNaissainId = ColumnExists(connection, "CommandesProduction", "TypeNaissainId");
            var hasTypeHuitreId = ColumnExists(connection, "CommandesProduction", "TypeHuitreId");

            migrationBuilder.Sql("PRAGMA foreign_keys=OFF;", suppressTransaction: true);

            if (hasTypesNaissain && !hasTypesHuitre)
            {
                migrationBuilder.Sql("ALTER TABLE TypesNaissain RENAME TO TypesHuitre;");
                hasTypesHuitre = true;
            }
            else if (!hasTypesNaissain && !hasTypesHuitre)
            {
                migrationBuilder.Sql("""
                    CREATE TABLE TypesHuitre (
                        Id INTEGER NOT NULL CONSTRAINT PK_TypesHuitre PRIMARY KEY AUTOINCREMENT,
                        Nom TEXT NOT NULL,
                        Actif INTEGER NOT NULL,
                        Ordre INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        CreatedByUserId INTEGER NULL);
                    """);
                hasTypesHuitre = true;
            }

            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TypesNaissain_Nom;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TypesNaissain_Ordre;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_TypesHuitre_Nom ON TypesHuitre (Nom);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_TypesHuitre_Ordre ON TypesHuitre (Ordre);");

            if (hasTypeNaissainId && !hasTypeHuitreId)
            {
                DropForeignKeyIfExists(connection, migrationBuilder, "FK_CommandesProduction_TypesNaissain_TypeNaissainId");
                migrationBuilder.Sql("ALTER TABLE CommandesProduction RENAME COLUMN TypeNaissainId TO TypeHuitreId;");
                hasTypeHuitreId = true;
            }

            if (hasTypeText && !hasTypeHuitreId)
            {
                migrationBuilder.Sql("ALTER TABLE CommandesProduction ADD COLUMN TypeHuitreId INTEGER NOT NULL DEFAULT 0;");
                migrationBuilder.Sql("""
                    UPDATE CommandesProduction
                    SET TypeHuitreId = COALESCE(
                        (SELECT t.Id FROM TypesHuitre t WHERE t.Nom = CommandesProduction.TypeHuitre LIMIT 1),
                        0);
                    """);
                migrationBuilder.Sql("ALTER TABLE CommandesProduction DROP COLUMN TypeHuitre;");
                hasTypeHuitreId = true;
            }

            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_CommandesProduction_TypeNaissainId;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_CommandesProduction_TypeHuitreId ON CommandesProduction (TypeHuitreId);");

            if (hasTypeHuitreId && hasTypesHuitre
                && !ForeignKeyExists(connection, "FK_CommandesProduction_TypesHuitre_TypeHuitreId"))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_CommandesProduction_TypesHuitre_TypeHuitreId",
                    table: "CommandesProduction",
                    column: "TypeHuitreId",
                    principalTable: "TypesHuitre",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            }

            migrationBuilder.Sql("PRAGMA foreign_keys=ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
            connection.Open();

            migrationBuilder.Sql("PRAGMA foreign_keys=OFF;", suppressTransaction: true);

            DropForeignKeyIfExists(connection, migrationBuilder, "FK_CommandesProduction_TypesHuitre_TypeHuitreId");

            if (ColumnExists(connection, "CommandesProduction", "TypeHuitreId")
                && !ColumnExists(connection, "CommandesProduction", "TypeNaissainId"))
            {
                migrationBuilder.Sql("ALTER TABLE CommandesProduction RENAME COLUMN TypeHuitreId TO TypeNaissainId;");
            }

            if (TableExists(connection, "TypesHuitre") && !TableExists(connection, "TypesNaissain"))
            {
                migrationBuilder.Sql("ALTER TABLE TypesHuitre RENAME TO TypesNaissain;");
            }

            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TypesHuitre_Nom;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_TypesHuitre_Ordre;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_CommandesProduction_TypeHuitreId;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_TypesNaissain_Nom ON TypesNaissain (Nom);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_TypesNaissain_Ordre ON TypesNaissain (Ordre);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_CommandesProduction_TypeNaissainId ON CommandesProduction (TypeNaissainId);");

            if (ColumnExists(connection, "CommandesProduction", "TypeNaissainId")
                && !ForeignKeyExists(connection, "FK_CommandesProduction_TypesNaissain_TypeNaissainId"))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_CommandesProduction_TypesNaissain_TypeNaissainId",
                    table: "CommandesProduction",
                    column: "TypeNaissainId",
                    principalTable: "TypesNaissain",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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

        private static void DropForeignKeyIfExists(SqliteConnection connection, MigrationBuilder migrationBuilder, string foreignKeyName)
        {
            if (!ForeignKeyExists(connection, foreignKeyName))
                return;

            migrationBuilder.DropForeignKey(foreignKeyName, "CommandesProduction");
        }

        private static bool ForeignKeyExists(SqliteConnection connection, string foreignKeyName)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'CommandesProduction' AND sql LIKE $pattern;";
            cmd.Parameters.AddWithValue("$pattern", $"%{foreignKeyName}%");
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
