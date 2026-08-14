using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwardDC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInwardReceivedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "InwardEntries",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "InwardEntries");
        }
    }
}
