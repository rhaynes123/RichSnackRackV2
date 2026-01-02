using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackRack.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoveHyphensFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 CREATE or REPLACE FUNCTION remove_hyphens(input text)
                                 RETURNS text
                                 LANGUAGE sql
                                 IMMUTABLE 
                                 AS $$
                                     select replace(input, '-','');
                                 $$;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 
                                 DROP FUNCTION IF Exists remove_hyphens;
                                 """);
        }
    }
}
