using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostApiService.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 6, 22, 6, 48, 313, DateTimeKind.Local).AddTicks(415));

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 6, 22, 6, 48, 313, DateTimeKind.Local).AddTicks(421));

            migrationBuilder.UpdateData(
                table: "Comments",
                keyColumn: "CommentId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2024, 10, 6, 22, 6, 48, 313, DateTimeKind.Local).AddTicks(425));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 1,
                column: "CreateAt",
                value: new DateTime(2024, 10, 6, 22, 6, 48, 313, DateTimeKind.Local).AddTicks(76));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 2,
                column: "CreateAt",
                value: new DateTime(2024, 10, 6, 22, 6, 48, 313, DateTimeKind.Local).AddTicks(137));
        }
    }
}
