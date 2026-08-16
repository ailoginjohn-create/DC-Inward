using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwardDC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserModuleRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedModules",
                table: "Users",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedModules",
                table: "Users");
        }
    }
}
