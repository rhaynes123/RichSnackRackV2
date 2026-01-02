using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnackRack.Migrations
{
    /// <inheritdoc />
    public partial class AddNewProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                     INSERT INTO products ("Id","Name", "Description", "Price", "ImageUrl", "IsActive")
                                     VALUES
                                         (
                                             uuid_generate_v4(),
                                             'Pop-Tarts',
                                             'Classic sweet snack',
                                             1.50,
                                             null,
                                             true
                                         );
                                         
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
