using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dizgem.Migrations
{
    /// <inheritdoc />
    public partial class FormsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormHandlers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UniqueIdentifier = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    ActionTarget = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SuccessMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormHandlers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormHandlers_UniqueIdentifier",
                table: "FormHandlers",
                column: "UniqueIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormHandlers");
        }
    }
}
