using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dizgem.Migrations
{
    /// <inheritdoc />
    public partial class PageTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageTag_Pages_PageId",
                table: "PageTag");

            migrationBuilder.DropForeignKey(
                name: "FK_PageTag_Tags_TagId",
                table: "PageTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PageTag",
                table: "PageTag");

            migrationBuilder.RenameTable(
                name: "PageTag",
                newName: "PageTags");

            migrationBuilder.RenameIndex(
                name: "IX_PageTag_TagId",
                table: "PageTags",
                newName: "IX_PageTags_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PageTags",
                table: "PageTags",
                columns: new[] { "PageId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PageTags_Pages_PageId",
                table: "PageTags",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PageTags_Tags_TagId",
                table: "PageTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageTags_Pages_PageId",
                table: "PageTags");

            migrationBuilder.DropForeignKey(
                name: "FK_PageTags_Tags_TagId",
                table: "PageTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PageTags",
                table: "PageTags");

            migrationBuilder.RenameTable(
                name: "PageTags",
                newName: "PageTag");

            migrationBuilder.RenameIndex(
                name: "IX_PageTags_TagId",
                table: "PageTag",
                newName: "IX_PageTag_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PageTag",
                table: "PageTag",
                columns: new[] { "PageId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PageTag_Pages_PageId",
                table: "PageTag",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PageTag_Tags_TagId",
                table: "PageTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
