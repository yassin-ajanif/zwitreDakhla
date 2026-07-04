using System;
using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefaultProductionLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
            connection.Open();

            var typesTable = ResolveTypesTable(connection);
            if (!string.IsNullOrEmpty(typesTable))
            {
                var typeFkColumn = ResolveTypeFkColumn(connection);
                if (!string.IsNullOrEmpty(typeFkColumn))
                {
                    migrationBuilder.Sql($"""
                        DELETE FROM {typesTable}
                        WHERE Nom IN ('Grand', 'Moyenne', 'Petit')
                          AND Id NOT IN (
                              SELECT DISTINCT {typeFkColumn}
                              FROM CommandesProduction
                              WHERE {typeFkColumn} IS NOT NULL AND {typeFkColumn} > 0);
                        """);
                }
                else
                {
                    migrationBuilder.Sql($"""
                        DELETE FROM {typesTable}
                        WHERE Nom IN ('Grand', 'Moyenne', 'Petit');
                        """);
                }
            }

            if (TableExists(connection, "CategoriesCommande"))
            {
                if (TableExists(connection, "CommandesProduction")
                    && ColumnExists(connection, "CommandesProduction", "CategorieCommandeId"))
                {
                    migrationBuilder.Sql("""
                        DELETE FROM CategoriesCommande
                        WHERE Nom IN ('Catégorie A', 'Catégorie B')
                          AND Id NOT IN (
                              SELECT DISTINCT CategorieCommandeId
                              FROM CommandesProduction
                              WHERE CategorieCommandeId IS NOT NULL AND CategorieCommandeId > 0);
                        """);
                }
                else
                {
                    migrationBuilder.Sql("""
                        DELETE FROM CategoriesCommande
                        WHERE Nom IN ('Catégorie A', 'Catégorie B');
                        """);
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            using var connection = new SqliteConnection(DatabasePath.GetConnectionString());
            connection.Open();

            var typesTable = ResolveTypesTable(connection);
            if (!string.IsNullOrEmpty(typesTable))
            {
                migrationBuilder.Sql($"""
                    INSERT INTO {typesTable} (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                    SELECT 'Grand', 1, 1, datetime('now'), datetime('now')
                    WHERE NOT EXISTS (SELECT 1 FROM {typesTable} WHERE Nom = 'Grand');

                    INSERT INTO {typesTable} (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                    SELECT 'Moyenne', 1, 2, datetime('now'), datetime('now')
                    WHERE NOT EXISTS (SELECT 1 FROM {typesTable} WHERE Nom = 'Moyenne');

                    INSERT INTO {typesTable} (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                    SELECT 'Petit', 1, 3, datetime('now'), datetime('now')
                    WHERE NOT EXISTS (SELECT 1 FROM {typesTable} WHERE Nom = 'Petit');
                    """);
            }

            if (TableExists(connection, "CategoriesCommande"))
            {
                migrationBuilder.Sql("""
                    INSERT INTO CategoriesCommande (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                    SELECT 'Catégorie A', 1, 1, datetime('now'), datetime('now')
                    WHERE NOT EXISTS (SELECT 1 FROM CategoriesCommande WHERE Nom = 'Catégorie A');

                    INSERT INTO CategoriesCommande (Nom, Actif, Ordre, CreatedAt, UpdatedAt)
                    SELECT 'Catégorie B', 1, 2, datetime('now'), datetime('now')
                    WHERE NOT EXISTS (SELECT 1 FROM CategoriesCommande WHERE Nom = 'Catégorie B');
                    """);
            }
        }

        private static string ResolveTypesTable(SqliteConnection connection)
        {
            if (TableExists(connection, "TypesHuitre"))
                return "TypesHuitre";

            if (TableExists(connection, "TypesNaissain"))
                return "TypesNaissain";

            return string.Empty;
        }

        private static string ResolveTypeFkColumn(SqliteConnection connection)
        {
            if (!TableExists(connection, "CommandesProduction"))
                return string.Empty;

            if (ColumnExists(connection, "CommandesProduction", "TypeHuitreId"))
                return "TypeHuitreId";

            if (ColumnExists(connection, "CommandesProduction", "TypeNaissainId"))
                return "TypeNaissainId";

            return string.Empty;
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
    }
}
