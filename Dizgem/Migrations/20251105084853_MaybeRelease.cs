using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dizgem.Migrations
{
    /// <inheritdoc />
    public partial class MaybeRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverPhotoUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CoverPhotoUrl",
                table: "Pages");

            migrationBuilder.AddColumn<Guid>(
                name: "CoverPhotoMediaId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CoverPhotoMediaId",
                table: "Pages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CoverPhotoMediaId",
                table: "Posts",
                column: "CoverPhotoMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_CoverPhotoMediaId",
                table: "Pages",
                column: "CoverPhotoMediaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Media_CoverPhotoMediaId",
                table: "Pages",
                column: "CoverPhotoMediaId",
                principalTable: "Media",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Media_CoverPhotoMediaId",
                table: "Posts",
                column: "CoverPhotoMediaId",
                principalTable: "Media",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Media_CoverPhotoMediaId",
                table: "Pages");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Media_CoverPhotoMediaId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CoverPhotoMediaId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Pages_CoverPhotoMediaId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "CoverPhotoMediaId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CoverPhotoMediaId",
                table: "Pages");

            migrationBuilder.AddColumn<string>(
                name: "CoverPhotoUrl",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverPhotoUrl",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
