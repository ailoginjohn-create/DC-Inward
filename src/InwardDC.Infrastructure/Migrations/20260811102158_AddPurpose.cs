using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InwardDC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurposeId",
                table: "InwardEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurposeId",
                table: "DispatchChallans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Purposes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purposes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InwardEntries_PurposeId",
                table: "InwardEntries",
                column: "PurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchChallans_PurposeId",
                table: "DispatchChallans",
                column: "PurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_Purposes_Name",
                table: "Purposes",
                column: "Name",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchChallans_Purposes_PurposeId",
                table: "DispatchChallans",
                column: "PurposeId",
                principalTable: "Purposes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InwardEntries_Purposes_PurposeId",
                table: "InwardEntries",
                column: "PurposeId",
                principalTable: "Purposes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchChallans_Purposes_PurposeId",
                table: "DispatchChallans");

            migrationBuilder.DropForeignKey(
                name: "FK_InwardEntries_Purposes_PurposeId",
                table: "InwardEntries");

            migrationBuilder.DropTable(
                name: "Purposes");

            migrationBuilder.DropIndex(
                name: "IX_InwardEntries_PurposeId",
                table: "InwardEntries");

            migrationBuilder.DropIndex(
                name: "IX_DispatchChallans_PurposeId",
                table: "DispatchChallans");

            migrationBuilder.DropColumn(
                name: "PurposeId",
                table: "InwardEntries");

            migrationBuilder.DropColumn(
                name: "PurposeId",
                table: "DispatchChallans");
        }
    }
}
