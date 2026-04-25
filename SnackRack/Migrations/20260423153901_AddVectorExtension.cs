using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace SnackRack.Migrations
{
    /// <inheritdoc />
    public partial class AddVectorExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "DescriptionEmbedding",
                table: "products",
                type: "vector(768)",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE INDEX ix_products_description_embedding_hnsw
                ON products
                USING hnsw ("DescriptionEmbedding" vector_cosine_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_description_embedding_hnsw;");

            migrationBuilder.DropColumn(
                name: "DescriptionEmbedding",
                table: "products");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS vector;");
        }
    }
}
