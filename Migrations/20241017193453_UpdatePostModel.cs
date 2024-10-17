using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostApiService.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Author",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 17, 22, 34, 51, 611, DateTimeKind.Local).AddTicks(8842));

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 17, 22, 34, 51, 611, DateTimeKind.Local).AddTicks(8854));

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 17, 22, 34, 51, 611, DateTimeKind.Local).AddTicks(8862));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 1,
                columns: new[] { "Author", "CreateAt", "Description", "ImageUrl", "MetaDescription", "MetaTitle", "Slug" },
                values: new object[] { "Peter Jack", new DateTime(2024, 10, 17, 22, 34, 51, 611, DateTimeKind.Local).AddTicks(8214), "Description for first post", "https://ibb.co/3M2k1wQ", "This is meta description", "Meta title info", "http://localhost:4200/first-post" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 2,
                columns: new[] { "Author", "CreateAt", "Description", "ImageUrl", "MetaDescription", "MetaTitle", "Slug" },
                values: new object[] { "Jay Way", new DateTime(2024, 10, 17, 22, 34, 51, 611, DateTimeKind.Local).AddTicks(8324), "Description for second post", "https://ibb.co/3M2k1wQ", "This is meta description 2", "Meta title info 2", "http://localhost:4200/second-post" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Author",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Posts");

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 6, 22, 7, 10, 703, DateTimeKind.Local).AddTicks(3504));

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 6, 22, 7, 10, 703, DateTimeKind.Local).AddTicks(3565));

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 6, 22, 7, 10, 703, DateTimeKind.Local).AddTicks(3570));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 1,
                column: "CreateAt",
                value: new DateTime(2024, 10, 6, 22, 7, 10, 703, DateTimeKind.Local).AddTicks(2401));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 2,
                column: "CreateAt",
                value: new DateTime(2024, 10, 6, 22, 7, 10, 703, DateTimeKind.Local).AddTicks(2487));
        }
    }
}
