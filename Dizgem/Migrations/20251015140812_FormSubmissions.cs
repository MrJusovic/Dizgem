using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dizgem.Migrations
{
    /// <inheritdoc />
    public partial class FormSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormHandlerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_FormHandlers_FormHandlerId",
                        column: x => x.FormHandlerId,
                        principalTable: "FormHandlers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormHandlerId",
                table: "FormSubmissions",
                column: "FormHandlerId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_Id",
                table: "FormSubmissions",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormSubmissions");
        }
    }
}
