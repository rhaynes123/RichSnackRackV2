using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackRack.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                     INSERT INTO products ("Id","Name", "Description", "Price", "ImageUrl", "IsActive")
                                     VALUES
                                         (
                                             uuid_generate_v4(),
                                             'Snickers',
                                             'Classic milk chocolate bar',
                                             1.50,
                                             null,
                                             true
                                         ),
                                         (
                                             uuid_generate_v4(),
                                             'Lays Chips',
                                             'Salted potato chips',
                                             2.25,
                                             null,
                                             true
                                         ),
                                         (
                                             uuid_generate_v4(),
                                             'Orange',
                                             'Sweet fruit',
                                             1.75,
                                             null,
                                             true
                                         );
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                     DELETE FROM products
                                     WHERE name IN (
                                         'Snickers',
                                         'Lays Chips',
                                         'Orange'
                                     );
                                 """);
        }
    }
}
