using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwardDC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchImportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNo",
                table: "DispatchChallans",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModeOfDispatch",
                table: "DispatchChallans",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "DispatchChallans",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PodNo",
                table: "DispatchChallans",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceNo",
                table: "DispatchChallans");

            migrationBuilder.DropColumn(
                name: "ModeOfDispatch",
                table: "DispatchChallans");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "DispatchChallans");

            migrationBuilder.DropColumn(
                name: "PodNo",
                table: "DispatchChallans");
        }
    }
}
