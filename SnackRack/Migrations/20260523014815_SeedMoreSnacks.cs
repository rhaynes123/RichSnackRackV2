using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackRack.Migrations
{
    /// <inheritdoc />
    public partial class SeedMoreSnacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO products ("Id", "Name", "Description", "Price", "ImageUrl", "IsActive", "DescriptionEmbedding")
                VALUES
                    (
                        uuid_generate_v4(),
                        'Cheez-It',
                        'Baked cheddar cheese crackers with a rich, sharp flavour',
                        2.00,
                        null,
                        true,
                        null
                    ),
                    (
                        uuid_generate_v4(),
                        'Reese''s Peanut Butter Cups',
                        'Milk chocolate cups filled with smooth, salty peanut butter',
                        1.75,
                        null,
                        true,
                        null
                    ),
                    (
                        uuid_generate_v4(),
                        'CLIF Bar',
                        'Oat-based energy bar with chocolate chips, packed with protein',
                        3.50,
                        null,
                        true,
                        null
                    ),
                    (
                        uuid_generate_v4(),
                        'Goldfish Crackers',
                        'Cheddar flavoured bite-sized baked fish-shaped snack crackers',
                        2.25,
                        null,
                        true,
                        null
                    ),
                    (
                        uuid_generate_v4(),
                        'Kind Bar',
                        'Whole almond and dark chocolate nut bar sweetened with honey',
                        3.00,
                        null,
                        true,
                        null
                    ),
                    (
                        uuid_generate_v4(),
                        'Smartfood Popcorn',
                        'Air-popped popcorn coated in white cheddar cheese seasoning',
                        2.75,
                        null,
                        true,
                        null
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM products
                WHERE "Name" IN (
                    'Cheez-It',
                    'Reese''s Peanut Butter Cups',
                    'CLIF Bar',
                    'Goldfish Crackers',
                    'Kind Bar',
                    'Smartfood Popcorn'
                );
                """);
        }
    }
}
